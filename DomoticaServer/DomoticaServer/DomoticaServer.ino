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
#define analogPin    0  // sensor value

byte mac[] = { 0x40, 0x6c, 0x8f, 0x36, 0x84, 0x8a };
EthernetServer server(3300);
NewRemoteTransmitter apa3Transmitter(unitCodeApa3, RFPin, 260, 3);
bool connected = false;

bool unit1 = false;
bool unit2 = false;
bool unit3 = false;

int led = 8;
int control = 0;
String val = "";

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  pinMode(led, OUTPUT);
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
    char inByte = ethernetClient.read();
    executeCommand(inByte);
    inByte = NULL;     
    }
}

void executeCommand(char cmd)
{
  char buf[4] = {'\0', '\0', '\0', '\0'};
  switch(cmd){
    case '1':
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
  
    case '2':
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
  
    case '3':
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

      case 'a':
        if(unit1)
        {
          Serial.println("Je bent nu bij pin1");
          server.write(" O1\n");
          Serial.println("Stopcontact 1 is aan!");  
        }
        else
        {
          server.write("F-1\n");
          Serial.println("Stopcontact 1 is uit!");
        }
      break;

      case 'b':
      if(unit2)
      {
        Serial.println("Je bent nu bij pin2");
        server.write(" O2\n");
        Serial.println("Stopcontact 2 is aan!");  
      }
      else
      {
        server.write("F-2\n");
        Serial.println("Stopcontact 2 is uit!");
      }
      break;

      case 'c':
      if(unit3)
      {
        Serial.println("Je bent nu bij pin3");
        server.write(" O3\n");
        Serial.println("Stopcontact 3 is aan!");  
      }
      else
      {
        server.write("F-3\n");
        Serial.println("Stopcontact 3 is uit!");
      }
      break;
  }
}

