from pyftdi import ftdi
import asyncio
import paho.mqtt.client as mqtt

#FTDI device info
vendor=0x0403
product=0x6001

topicTemplateCount = 9 # /Theater/

########
# Device
########
class Device():
    def __init__(self, rawData):
        self.name = rawData
        self.id = int(rawData[len(rawData) - 2: len(rawData)])
        self.deviceType = rawData[0 : len(rawData) - 3]
        self.topic = "/Theater/" + rawData

####################
# DMX USB controller
####################
class OpenDmxUsb():
    def __init__(self):
        self.baud_rate = 250000
        self.data_bits = 8
        self.stop_bits = 2
        self.parity = 'N'
        self.flow_ctrl = ''
        self.rts_state = 0
        self._init_dmx()

    #Initialize the controller
    def _init_dmx(self):
        self.ftdi=ftdi.Ftdi()
        self.ftdi.open(vendor,product,0)
        if self.ftdi.is_connected:
            print("ftdi connected")
        else:
            print("ftdi not connected")
        self.ftdi.set_baudrate(self.baud_rate)
        self.ftdi.set_line_property(self.data_bits,
                   self.stop_bits,
                   self.parity,
                   break_=0)
        self.ftdi.set_flowctrl(self.flow_ctrl)
        self.ftdi.purge_rx_buffer()
        self.ftdi.purge_tx_buffer()
        self.ftdi.set_rts(self.rts_state)

    #Send DMX data
    def send_dmx(self,channelVals):
        #Need to generate two bits for
        dmx.ftdi.purge_rx_buffer()
        dmx.ftdi.purge_tx_buffer() 
        self.ftdi.set_line_property(self.data_bits,
                   self.stop_bits,
                   self.parity,
                   break_=1)
        self.ftdi.set_line_property(self.data_bits,
                    self.stop_bits,
                    self.parity,
                    break_=1)
        self.ftdi.set_line_property(self.data_bits,
                    self.stop_bits,
                    self.parity,
                    break_=0)
        self.ftdi.write_data(channelVals)
          


dmx = OpenDmxUsb()

########################
# Read available Devices
########################
path = "Devices.txt"
#path = "..\Interface\TheaterControl.Interface\Configuration\Devices.txt"
file = open(path, "r")
lines = file.readlines()
devices = []
for line in lines:
    devices.append(Device(line.strip().lower()))

# Initial data

#channel 0  1  2
#Der erste muss 0 sein
rList = [0, 0, 0]
channel_vals = bytearray(rList)
for x in bytearray([0]*510):
    channel_vals.append(0)

# Queue to change the data sent to the adapter
valueChangesQueue = []


###################
# MQTT Client Setup
###################

def on_connect(client, userdata, flags, rc):
    global devices
    print("mqtt connected")
    for device in devices:
        client.subscribe(device.topic)
        print(device.topic)
    
def on_message(client, userdata, msg):
    global valueChangesQueue
    global devices
    found = next(device for device in devices if device.topic == msg.topic)
    if found is not None:
        value= int(msg.payload.decode("utf-8"))
        if value > 100:
            value = 100
        elif value < 0:
            value = 0
        value = int(value * 255 / 100)
        valueChangesQueue.append([found.id,value])


client = mqtt.Client(transport="websockets")

client.on_message = on_message
client.on_connect = on_connect
client.on_disconnect = print("mqtt disconnected")
#Kabel
#client.connect("169.254.143.232", 9001, 60)
#client.connect("192.168.178.52", 9001, 60)
#client.connect("192.168.43.1", 9001, 60)
client.connect("192.168.51.1", 9001, 60)
client.loop_start()

def main():
    while True :
        global valueChangesQueue 
        item = []
        if len(valueChangesQueue) > 0:
            item = valueChangesQueue.pop()
        if not item == []:
            print("Item:")
            print(item)
            channel_vals[item[0]] = item[1]
        dmx.send_dmx(channel_vals)
    
asyncio.ensure_future(main())