import paho.mqtt.client as mqtt, os, asyncio, time

SCENE_PATH = "./Configuration/Scenes.txt"
DEVICE_PATH = "./Configuration/Devices.txt"
SONG_PATH = "./Music"
TOPIC_BASE = "/Theater/"
SCENE_CONFIGURATION_TOPIC = "/Theater/Scene"
SCENE_CONTROL_TOPIC = "/Theater/Control"
SONG_CONTROL_TOPIC_FROM_UI = "/Theater/SongControlUI"
SELECTION_TOPIC = "/Theater/Selection"
DEVICE_TOPIC = "/Theater/Device"
SONG_TOPIC = "/Theater/songs"
SONG_CONTROL_TOPIC_FROM_INTERFACE = "/Theater/SongControlInterface"

SCENES_CHANGED_PAYLOAD = "ScenesChanged"
DEVICE_OFF_PAYLOAD = "0"

SERVER_ADDRESS = "localhost"

LENGTH_OF_TIME_SKIP = "10"
KEYWORD_DURATION = "Duration"

scenes = []
devices = []
songs = []

selectedScene = None
selectedSong = None

executionQueue = []

runningSceneTask = None
cancelSong = False

#################
# Reading Files #
#################

def readFile(path):
    try:
        file = open(path, encoding='utf-8-sig')
    except FileNotFoundError:
        print("\nFile at \" " + path + " \" not found.")
        print("Press enter to exit the program...\n")
        input()
        exit()
    lines = file.readlines()
    stripped = []
    for line in lines:
        stripped.append(line.strip())
    file.close()
    return stripped

def readDevices():
    return readFile(DEVICE_PATH)

def readSongs():
    filenames = os.listdir(SONG_PATH)
    return filenames

def readScenes(allPossibleDevices):
    scenes = []
    rawSceneDescriptions = readFile(SCENE_PATH)
    for desc in rawSceneDescriptions:
        if desc.strip().startswith("//") or desc == "":
            continue
        lineElements = desc.split(';')
        sceneDescription = lineElements[0].strip()
        devicesOfScene = []
        duration = -1
        for d in lineElements:
            nameValuePair = d.strip().split(' ')
            if nameValuePair[0].lower() in allPossibleDevices:
                devicesOfScene.append(Device(nameValuePair[0], nameValuePair[1]))
            if nameValuePair[0].startswith(KEYWORD_DURATION):
                duration = nameValuePair[1]
        scene = Scene(sceneDescription, devicesOfScene)
        scene.duration = int(duration)
        scenes.append(scene)
    return scenes

def readDatafromFiles():

    global scenes
    global devices
    global songs

    songs = readSongs()
    devices = readDevices()
    scenes = readScenes(devices)

########################
# Monitor File Changes #
########################
# Not implemented yet
###########
# Classes #
###########

class Scene:
    duration = -1
    def __init__(self, description, devices):
        self.description = description
        self.devices = devices

class Device:
    def __init__(self, name, value):
        self.name = name
        self.value = value
        self.topic = TOPIC_BASE + name

############
# Controls #
############

async def cancellableTimer(seconds):
    await asyncio.sleep(seconds)

def cancelTimer():
    global runningSceneTask
    if runningSceneTask == None:
        runningSceneTask.cancel()
    runningSceneTask = None

def addSuccessCallback(task, loop, callback, params):
    result = loop.run_until_complete(asyncio.wait(task))
    cancelTimer()
    stopMusic()
    callback(params)

def playNextScene():
    global selectedScene
    global scenes
    descriptions = []
    for d in scenes:
        descriptions.append(d.description)
    nextIndex = descriptions.index(selectedScene.description) + 1
    stopMusic()
    sendMessageToServer(SELECTION_TOPIC, "s" + str(nextIndex))
    sendMessageToServer(SCENE_CONTROL_TOPIC, "Play")

def stopMusic():
    global selectedScene
    sendMessageToServer(SONG_CONTROL_TOPIC_FROM_INTERFACE, DEVICE_OFF_PAYLOAD)


def playScene():
    global selectedScene
    global selectedSong
    global runningSceneTask
    global cancelSong
    cancelSong = True
    stopMusic()
    print("play scene " + selectedScene.description[0:4])
    for device in selectedScene.devices:
        if device.name.lower() == "music":
            selectedSong = str(device.value)
            sendMessageToServer(SONG_CONTROL_TOPIC_FROM_INTERFACE, device.value)
            continue
        sendMessageToServer(device.topic, str(device.value))

    if selectedScene.duration > 0:
        print(selectedScene.description[0:4] + " has duration " + str(selectedScene.duration))
        cancelSong = False
        compareTimer(selectedScene.duration)

def compareTimer(duration):
    global cancelSong
    start = time.time()
    timeRunning = 0
    while timeRunning < duration and cancelSong == False:
        timeRunning = time.time() - start
    cancelSong = False
    playNextScene()


def updateUI():
    readDatafromFiles()
    publishScenesAndDevicesToUI()

def emergencyStop():
    global executionQueue
    global scenes
    global runningSceneTask
    if runningSceneTask != None:
        cancelTimer()
    for scene in scenes:
        for device in scene.devices:
            sendMessageToServer(device.topic, DEVICE_OFF_PAYLOAD)
    executionQueue = []

def changeSceneSelection(value):
    global scenes
    global selectedScene
    index = None
    try:
        index = int(value, 10)
    except ValueError:
        return
    sceneDescriptions = []
    for scene in scenes:
        sceneDescriptions.append(scene.description)
    if index != None and index != sceneDescriptions.index(selectedScene.description) and index < len(scenes) and index >= 0:
        selectedScene = scenes[index]
   

def changeSongSelection(value):
    global songs
    global selectedSong

    index = None
    try:
        index = int(value, 10)
    except ValueError:
        return
    songDescriptions = []
    for song in songs:
        songDescriptions.append(song.lower())
    if index != None and index != songDescriptions.index(selectedSong.lower()) and index < len(songDescriptions) and index >= 0:
        selectedSong = songs[index]

def selectionChanged(payload):
    if(payload[0] == 's'):
        changeSceneSelection(payload[1])
    if(payload[0] == 'm'):
        changeSongSelection(payload[1])

def runSongBack():
    payload = "RunBackTime " + LENGTH_OF_TIME_SKIP
    sendMessageToServer(SONG_CONTROL_TOPIC_FROM_INTERFACE, payload)

def pauseSong():
    global selectedSong
    if selectedSong == None:
        return
    sendMessageToServer(SONG_CONTROL_TOPIC_FROM_INTERFACE, selectedSong)

def runSongForward():
    payload = "RunForwardTime " + LENGTH_OF_TIME_SKIP
    sendMessageToServer(SONG_CONTROL_TOPIC_FROM_INTERFACE, payload)

controls = {
    "Play" : playScene,
    "Stop" : stopMusic,
    "Emergency" : emergencyStop,
    "RequestScenes" : updateUI,
    "RunBack" : runSongBack,
    "Pause" : pauseSong,
    "RunForward" : runSongForward,
    "SelectionChanged" : selectionChanged,
}
#####################
# MQTT Client Setup #
#####################
def on_connect(client, userdata, flags, rc):
    client.subscribe(SCENE_CONFIGURATION_TOPIC)
    client.subscribe(SCENE_CONTROL_TOPIC)
    client.subscribe(SONG_CONTROL_TOPIC_FROM_UI)
    client.subscribe(SELECTION_TOPIC)
    print("mqtt connected")
    
    
def on_message(client, userdata, msg):
    global controls
    global executionQueue
    payload = msg.payload.decode("utf-8")
    if msg.topic == SELECTION_TOPIC:
        executionQueue.append(["SelectionChanged", payload])
        return
    if payload in controls:
        executionQueue.append([payload, None])

client = mqtt.Client(client_id="TheaterControlInterface")


client.on_message = on_message
client.on_connect = on_connect
client.on_disconnect = print("mqtt disconnected")

client.connect(SERVER_ADDRESS, 1883, 60)
client.loop_start()

##############
# Publishing #
##############

def sendMessageToServer(topic, payload):
    global client
    client.publish(topic,payload)

def publishScenesAndDevicesToUI():
    global scenes
    global devices
    global songs

    sceneDescriptions = []
    for scene in scenes:
        sceneDescriptions.append(scene.description)
    sceneMessage = ";;;".join(sceneDescriptions)
    deviceMessage = ";;;".join(devices)
    songMessage = ";;;".join(songs)

    sendMessageToServer(SCENE_CONFIGURATION_TOPIC, sceneMessage)
    sendMessageToServer(DEVICE_TOPIC, deviceMessage)
    sendMessageToServer(SONG_TOPIC, songMessage)

def workQueue():
    global executionQueue
    global controls
    while True :
            global executionQueue 
            item = []
            if len(executionQueue) > 0:
                item = executionQueue.pop()
            if not item == []:
                if item[1] != None:
                    controls[item[0]](item[1])
                else:
                    controls[item[0]]()
                

readDatafromFiles()

if len(scenes) == 0:
    print("\nNo Scenes described.")
    print("Press enter to stop the program...\n")
    input()
    exit()

selectedScene = scenes[0]
selectedSong = songs[0]

publishScenesAndDevicesToUI()

asyncio.ensure_future(workQueue())
