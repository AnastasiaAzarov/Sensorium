// ARDUINO UNO SKETCH (Connected to Unity on PORT 2)

const int temp = A4;   // Thermistor (Analog)
const int red = 13;
const int green = 12;
const int blue = 11;

void setup() {
  pinMode(red, OUTPUT);
  pinMode(green, OUTPUT);
  pinMode(blue, OUTPUT);
  // IMPORTANT: Match this baud rate in Unity
  Serial.begin(9600);
  Serial.println("Uno Thermistor Ready");
}

void loop() {
  // 1. Read Thermistor
  int tempReading = analogRead(temp);
  if(tempReading == 20){
    digitalWrite(green, HIGH);
  }
  else{
    digitalWrite(green, LOW);
  }
  if(tempReading < 20){
    digitalWrite(blue, HIGH);
  }
  else{
    digitalWrite(blue, LOW);
  }
  if(tempReading > 20){
    digitalWrite(red, HIGH);
  }
  else{
    digitalWrite(red, LOW);
  }

  // OUTPUT FORMAT: Temp (1 integer)
  Serial.println(tempReading); // newline-terminated

  // Delay for sampling rate
  delay(50); 
}