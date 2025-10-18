// ARDUINO SKETCH (Feather Sense - Connected to Unity on PORT 1)
// Sends Joystick, Photoresistor, and Sound data (5 values).

#include <bluefruit.h>

// ARDUINO SKETCH (Feather Sense - Connected to Unity on PORT 1)
// Sends Joystick (X, Y, Button), Photoresistor (Light), and Sound Sensor (Sound) data.

const int flashlight = 13;
// Joystick Pins
const int SW_pin = 10;   // Joystick Button (Digital)
const int X_pin = A1;   // Joystick X-axis (Analog)
const int Y_pin = A2;   // Joystick Y-axis (Analog)

// Sensor Pins
const int LIGHT_pin = A3;  // Photoresistor (Analog)
const int SOUND_digital = 11;
const int SOUND_pin = A0;  // Sound Sensor (Analog)

void setup() {
  // IMPORTANT: Match this baud rate in Unity
  Serial.begin(9600);

  // Set up pins
  pinMode(SW_pin, INPUT_PULLUP); // Joystick button setup: pressed == LOW (0)
  pinMode(flashlight, OUTPUT);

  Serial.println("Feather Controls Ready");
}

void loop() {
  // 1. Read Joystick
  int xVal = analogRead(X_pin);
  int yVal = analogRead(Y_pin);
  int switchState = digitalRead(SW_pin) == LOW ? 0 : 1; // 0 for pressed
  if(switchState == LOW){
    digitalWrite(flashlight, HIGH);
  }
  else{
    digitalWrite(flashlight, LOW);
  }

  // 2. Read Sensors
  int lightVal = analogRead(LIGHT_pin); 
  int soundVal = analogRead(SOUND_pin); 

  // OUTPUT FORMAT: X Y Button Light Sound (5 space-separated integers)
  Serial.print(xVal);
  Serial.print(" ");
  Serial.print(yVal);
  Serial.print(" ");
  Serial.print(switchState);
  Serial.print(" ");
  Serial.print(lightVal);
  Serial.print(" ");
  Serial.println(soundVal); // newline-terminated

  // Delay for sampling rate
  delay(30); // ~33 FPS data rate
}