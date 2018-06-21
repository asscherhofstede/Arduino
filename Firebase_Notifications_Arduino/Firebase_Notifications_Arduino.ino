//Includes
#include <SoftwareSerial.h>
#include <Time.h>
#include <TimeLib.h>
#include <OneWire.h>
#include <DallasTemperature.h>

//Define pins
#define tempPin 7

#define echoPin 9
#define trigPin 10

#define ledPin 11 //moet nog even kijken welke pins dit zijn
#define magnet 12 //moet nog even kijken welke pins dit zijn


//Variables
int countKliko = 0;
int countKoelkast = 0;
int minutes = 0;

OneWire oneWire(tempPin);
DallasTemperature sensors(&oneWire);
float temperatuur = 0;


//Instantiate serial communication between arduino and esp8266 module
SoftwareSerial Arduino(2, 3); //RX || TX


void setup() {
  Serial.begin(115200);
  Arduino.begin(115200);
  sensors.begin();

  //pinmodes
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);
  
  pinMode(ledPin, OUTPUT);
  pinMode(magnet, INPUT);


  //Functions
  setTime(9,0,0,1,1,18);
  
  digitalWrite(magnet, HIGH);
}

void loop() {
  //Eerst alle methods laten lopen?
  //Of per onderdeel eerst de method en dan de if statement?
  //Of in je method al de if statement --> ziet er naar mijn mening beter uit.
  Kliko();
  delay(200);
  //Koelkast();
  delay(200);
  //Ventilator();
  delay(200);
  //Wasmand();
  delay(200);
  //KoffieZetApparaat(); 
  delay(200);
}

void Kliko(){  
  long duration, distance;
  digitalWrite(trigPin, LOW); 
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);
  duration = pulseIn(echoPin, HIGH);
  distance = (duration/2) / 29.1;

  if (distance > 30 || distance <= 0){
    if (countKliko <= 60){
    Serial.print("Kliko is ");
    Serial.print(countKliko);
    Serial.println(" seconde weg");
    }
    if (countKliko >= 60){
      minutes = countKliko / 60;
      Serial.print("Kliko is al ");
      Serial.print(minutes);
      Serial.println(" minuut/minuten weg");
    }
    countKliko++;
  }
  else {
   countKliko = 0;
   Serial.println("Kliko staat op zijn plek :D");
  }
  
  if(countKliko == 60 && weekday() == 2 && hour() >= 9)
  {
    Serial.println("Zet de kliko aan de weg!");
    Arduino.print('a'); 
  } 
 
}

void Koelkast(){
  if(digitalRead(magnet) == HIGH)
  {
    countKoelkast++;
    if ( countKoelkast >= 10)
    {
    digitalWrite(ledPin, HIGH);
    } 
  }
  else
  {
    digitalWrite(ledPin, LOW);
    countKoelkast = 0;
  }
}

void Ventilator(){
  sensors.requestTemperatures();
  temperatuur = sensors.getTempCByIndex(0);
  
  if(temperatuur > 25){
    //Arduino.print('c');
    //het stopcontact aanzetten
  }
}

void Wasmand(){
  
}

void KoffieZetApparaat(){
  
}

