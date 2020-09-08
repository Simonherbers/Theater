from pyftdi import ftdi
from multiprocessing import Process, Queue
import asyncio, time, threading
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
        self.id = rawData[len(rawData) - 3: len(rawData) - 1]
        self.deviceType = rawData[0 : len(rawData) - 3]

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
        #self._init_dmx()

    #Initialize the controller
    def _init_dmx(self):
        self.ftdi=ftdi.Ftdi()
        self.ftdi.open(vendor,product,0)
        if self.ftdi.is_connected:
            print("connected")
        else:
            print("not connected")
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
        #Need to generate two bits for break
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

path = "..\Interface\TheaterControl.Interface\Configuration\Devices.txt"
file = open(path, "r")
lines = file.readlines()
devices = []
for line in lines:
    devices.append(Device(line.strip()))

print(len(devices))

# Initial data

#channel 0  1  2
rList = [0, 0, 0]
channel_vals = bytearray(rList)
for x in bytearray([0]*510):
    channel_vals.append(0)

# Queue to change the data sent to the adapter
valueChangesQueue = Queue()

#############################################
# Process to permanently send data to adapter
#############################################
def coro(dmx) :
    global valueChangesQueue
    while True :
        write(dmx)
        if valueChangesQueue.qsize() > 0:
            item = valueChangesQueue.get()
            channel_vals[item[0]] = item[1]
        
        
def write(dmx):
    global channel_vals
    # dmx.ftdi.set_break(True)
    # dmx.ftdi.set_break(False)

    #temporarily commented
    # dmx.ftdi.purge_rx_buffer()
    # dmx.ftdi.purge_tx_buffer()
    # dmx.send_dmx(channel_vals)

if __name__ == '__main__':    
    p2 = Process(target=coro, args=(dmx,))
    p2.start()



###################
# MQTT Client Setup
###################

def on_connect(client, userdata, flags, rc):
    global devices
    print("mqtt connected")
    for device in devices:
        print(device.name)
        client.subscribe(device.name)
    
def on_message(client, userdata, msg):
    global valueChangesQueue
    global devices
    found = next(device for device in devices if device.name == msg.topic[topicTemplateCount - 1 : len(msg.topic)])
    if found is not None:
        valueChangesQueue.put([int(found.id),int(str(msg.payload))])
        print(found.name + str(msg.payload))

if __name__ == '__main__':    
    p2 = Process(target=coro, args=(dmx,))
    p2.start()
client = mqtt.Client(transport="websockets")

client.on_message = on_message
client.on_connect = on_connect
client.on_disconnect = print("disconnected")

client.connect("169.254.143.232", 9001, 60)

client.loop_forever()
# print("wait")
# time.sleep(5)

# p2.terminate()
# print("done")