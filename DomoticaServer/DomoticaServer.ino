#include <SPI.h>
#include <Ethernet.h>
#include <NewRemoteTransmitter.h>

#define unitCodeApa3      29362034
#define RFPin        3  // output, pin to control the RF-sender (and Click-On Click-Off-device)
#define lowPin       5  // output, always LOW
#define highPin      6  // output, always HIGH
#define switchPin    7  // input, connected to some kind of inputswitch
#define ledPin       8  // output, led used for "connect state": blinking = searching; continuously = connected
#define infoPin      9  // output, more information
#define analogPin    A0  // sensor value
#define analogPin2   A1
byte mac[] = { 0x40, 0x6c, 0x8f, 0x36, 0x84, 0x8a };
EthernetServer server(3300);
NewRemoteTransmitter apa3Transmitter(unitCodeApa3, RFPin, 260, 3);
bool connected = false;

bool unit1 = false;
bool unit2 = false;
bool unit3 = false;
int sensorValue1 = 0;
int sensorValue2 = 0;
int hertz = 1;
String hz;
int refreshrate;
char buf[4] = {'\0', '\0', '\0', '\0'};

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  if(Ethernet.begin(mac) == 0){
    return;
  }

  Serial.print("Listening on address: ");
  Serial.println(Ethernet.localIP());
  server.begin();
  connected = true; 
}

void loop() {
  // put your main code here, to run repeatedly:
  if(!connected) {
    return;
  }
  
  EthernetClient ethernetClient = server.available();
  
  if(!ethernetClient){
    return;
  }
  while(ethernetClient.connected())
  {
    sensorValue1 = readSensor(A0, 100);
    sensorValue2 = readSensor(A1, 100);
    char inByte = ethernetClient.read();
    executeCommand(inByte);
    inByte = NULL;
  }
}

void executeCommand(char cmd)
{
  
  switch(cmd){
    case 'a':
      if(unit1)
          {
            apa3Transmitter.sendUnit(0, 0);\
            unit1 = false;
          }
          else
          {
            apa3Transmitter.sendUnit(0, 1);
            unit1 = true;
          } 
      break;
  
    case 'b':
      if(unit2)
          {
            apa3Transmitter.sendUnit(1, 0);\
            unit2 = false;
          }
          else
          {
            apa3Transmitter.sendUnit(1, 1);
            unit2 = true;
          } 
      break;
  
    case 'c':
      if(unit3)
          {
            apa3Transmitter.sendUnit(2, 0);\
            unit3 = false;
          }
          else
          {
            apa3Transmitter.sendUnit(2, 1);
            unit3 = true;
          } 
      break;

      case 'd':
        if(unit1)
        {
          server.write(" O1\n");
        }
        else
        {
          server.write("F-1\n");
        }
      break;

      case 'e':
      if(unit2)
      {
        server.write(" O2\n");
      }
      else
      {
        server.write("F-2\n");
      }
      break;

      case 'f':
      if(unit3)
      {
        server.write(" O3\n");
      }
      else
      {
        server.write("F-3\n");
      }
      break;

      case 'g':
        intToCharBuf(sensorValue1, buf, 4);
        server.write(buf, 4);
      break;

      case 'h':
        intToCharBuf(sensorValue2, buf, 4);
        server.write(buf, 4);
      break;
  }
}

void RefreshValue(int hertz)
{
  delay(1000/(hertz * 1000));
}

// Convert int <val> char buffer with length <len>
void intToCharBuf(int val, char buf[], int len)
{
   String s;
   s = String(val);                        // convert tot string
   if (s.length() == 1) s = "0" + s;       // prefix redundant "0" 
   if (s.length() == 2) s = "0" + s;  
   s = s + "\n";                           // add newline
   s.toCharArray(buf, len);                // convert string to char-buffer
}

// read value from pin pn, return value is mapped between 0 and mx-1
int readSensor(int pn, int mx)
{
  return map(analogRead(pn), 0, 1023, 0, mx-1);    
}

