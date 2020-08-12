import sys, os, pathlib, paho.mqtt.client as mqtt, vlc

SONG_CONTROL_TOPIC_FROM_INTERFACE = "/Theater/SongControlInterface"
STOP_PAYLOAD = "0"
musicDirectory = pathlib.Path("Music")
player = None
task = None
def on_connect(client, userdata, flags, rc):
    print("connected")
    client.subscribe("/Theater/music")
    client.subscribe(SONG_CONTROL_TOPIC_FROM_INTERFACE)

def on_message(client, userdata, msg):
    global player
    value = msg.payload.decode("utf-8")
    topic = msg.topic
    
    if topic == SONG_CONTROL_TOPIC_FROM_INTERFACE:
        HandleSongControl(value)
        return

    if value == STOP_PAYLOAD:
        if player is not None:
            player.stop()
    else:
        path = os.path.join(musicDirectory, value)
        player = vlc.MediaPlayer(path)
        player.play()

def HandleSongControl(value):
    global player
    values = str(value).split()

    if values[0] == "TogglePause" :
        player.pause()
        return

    val = player.get_time()
    
    if values[0] == "RunBackTime" :
        toRun = val - float(values[1]) * 1000
        if toRun < 0 :
            player.set_time(0)
        else :
            player.set_time(int(toRun))
        return

    if values[0] == "RunForwardTime" :
        toRun = val + float(values[1]) * 1000
        len = player.get_length()
        if toRun > len:
            player.stop()
        else:
            player.set_time(int(toRun))
            


        

client = mqtt.Client()

client.on_message = on_message
client.on_connect = on_connect

client.connect("linvm2416", 1883, 60)

client.loop_forever()