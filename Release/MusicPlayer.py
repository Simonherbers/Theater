import sys, os, pathlib, paho.mqtt.client as mqtt, vlc, asyncio

SONG_CONTROL_TOPIC_FROM_INTERFACE = "/Theater/SongControlInterface"
STOP_PAYLOAD = "0"
musicDirectory = pathlib.Path("Music")
player = None
runningSong = ""
queue = []

def on_connect(client, userdata, flags, rc):
    print("connected")
    #client.subscribe("/Theater/music")
    client.subscribe(SONG_CONTROL_TOPIC_FROM_INTERFACE)

def on_message(client, userdata, msg):
    global queue
    value = msg.payload.decode("utf-8")
    topic = msg.topic
    queue.append([topic, value])

def playSong(songName):
    global player
    global runningSong
    if player is not None and player.is_playing:
        player.stop()
    try:
        path = os.path.join(musicDirectory, songName)
        player = vlc.MediaPlayer(path)
        player.play()
        runningSong = songName
    except OSError as error:
        print(error)
        

def HandleSongControl(value):
    global player
    values = str(value).split()
    if values[0] == STOP_PAYLOAD:
        if player != None and player.is_playing:
            player.pause()
        return
    
    if values[0] == "RunBackTime" :
        val = player.get_time()
        toRun = val - float(values[1]) * 1000
        if toRun < 0 :
            player.set_time(0)
        else :
            player.set_time(int(toRun))
        return

    if values[0] == "RunForwardTime" :
        val = player.get_time()
        toRun = val + float(values[1]) * 1000
        len = player.get_length()
        if toRun > len:
            player.stop()
        else:
            player.set_time(int(toRun))
        return
    
    if values[0] == runningSong and player.is_playing:
        player.pause()
    else:
        playSong(values[0])

def WorkQueue():
    global player
    global queue
    while True:
        if(len(queue) == 0):
            continue
        message = queue.pop()
        if message[0] == SONG_CONTROL_TOPIC_FROM_INTERFACE:
            HandleSongControl(message[1])
            continue
        

client = mqtt.Client()

client.on_message = on_message
client.on_connect = on_connect
client.on_disconnect = print("disconnected")

client.connect("localhost", 1883, 60)

client.loop_start()

asyncio.ensure_future(WorkQueue())
