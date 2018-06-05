// Xamarin/C# app voor de besturing van een Arduino (Uno met Ethernet Shield) m.b.v. een socket-interface.
// Dit programma werkt samen met het Arduino-programma DomoticaServer.ino
// De besturing heeft betrekking op het aan- en uitschakelen van een Arduino pin, waar een led aan kan hangen of, 
// t.b.v. het Domotica project, een RF-zender waarmee een klik-aan-klik-uit apparaat bestuurd kan worden.
//
// De socket-communicatie werkt in is gebaseerd op een timer, waarbij het opvragen van gegevens van de 
// Arduino (server) m.b.v. een Timer worden gerealisseerd.
//
// Werking: De communicatie met de (Arduino) server is gebaseerd op een socket-interface. Het IP- en Port-nummer
// is instelbaar. Na verbinding kunnen, middels een eenvoudig commando-protocol, opdrachten gegeven worden aan 
// de server (bijv. pin aan/uit). Indien de server om een response wordt gevraagd (bijv. led-status of een
// sensorwaarde), wordt deze in een 4-bytes ASCII-buffer ontvangen, en op het scherm geplaatst. Alle commando's naar 
// de server zijn gecodeerd met 1 char.
//
// Aanbeveling: Bestudeer het protocol in samenhang met de code van de Arduino server.
// Het default IP- en Port-nummer (zoals dat in het GUI verschijnt) kan aangepast worden in de file "Strings.xml". De
// ingestelde waarde is gebaseerd op je eigen netwerkomgeving, hier (en in de Arduino-code) is dat een router, die via DHCP
// in het segment 192.168.1.x IP-adressen uitgeeft.
// 
// Resource files:
//   Main.axml (voor het grafisch design, in de map Resources->layout)
//   Strings.xml (voor alle statische strings in het interface (ook het default IP-adres), in de map Resources->values)
// 
// De software is verder gedocumenteerd in de code. Tijdens de colleges wordt er nadere uitleg over gegeven.
// 
// Versie 1.2, 16/12/2016
// S. Oosterhaven
//
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Android.Graphics;
using System.Threading.Tasks;
using Android.Util;

namespace Domotica
{
    [Activity(Label = "@string/application_name", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class MainActivity : Activity
    {
        // Variables (components/controls)
        // Controls on GUI
        Button buttonConnect, buttonChangePinState1, buttonChangePinState2, buttonChangePinState3, refreshButton;
        TextView textViewServerConnect, textViewTimerStateValue;
        public TextView textViewChangePinStateValue1, textViewSensorValue1, textViewDebugValue1;
        public TextView textViewChangePinStateValue2, textViewSensorValue2, textViewDebugValue2;
        public TextView textViewChangePinStateValue3, textViewSensorValue3, textViewDebugValue3;
        EditText editTextIPAddress, editTextIPPort, refreshTextInput;

        Timer timerClock, timerSockets;             // Timers   
        Socket socket = null;                       // Socket   
        List<Tuple<string, TextView>> commandList = new List<Tuple<string, TextView>>();  // List for commands and response places on UI
        int listIndex = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource (strings are loaded from Recources -> values -> Strings.xml)
            SetContentView(Resource.Layout.Main);

            // find and set the controls, so it can be used in the code
            buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);
            buttonChangePinState1 = FindViewById<Button>(Resource.Id.buttonChangePinState1);
            buttonChangePinState2 = FindViewById<Button>(Resource.Id.buttonChangePinState2);
            buttonChangePinState3 = FindViewById<Button>(Resource.Id.buttonChangePinState3);
            refreshButton = FindViewById<Button>(Resource.Id.refreshButton);

            textViewTimerStateValue = FindViewById<TextView>(Resource.Id.textViewTimerStateValue);
            textViewServerConnect = FindViewById<TextView>(Resource.Id.textViewServerConnect);
            textViewChangePinStateValue1 = FindViewById<TextView>(Resource.Id.textViewChangePinStateValue1);
            textViewChangePinStateValue2 = FindViewById<TextView>(Resource.Id.textViewChangePinStateValue2);
            textViewChangePinStateValue3 = FindViewById<TextView>(Resource.Id.textViewChangePinStateValue3);

            textViewSensorValue1 = FindViewById<TextView>(Resource.Id.textViewSensorValue1);
            textViewDebugValue1 = FindViewById<TextView>(Resource.Id.textViewDebugValue1);
            textViewSensorValue2 = FindViewById<TextView>(Resource.Id.textViewSensorValue2);
            textViewDebugValue2 = FindViewById<TextView>(Resource.Id.textViewDebugValue2);
            textViewSensorValue3 = FindViewById<TextView>(Resource.Id.textViewSensorValue3);
            textViewDebugValue3 = FindViewById<TextView>(Resource.Id.textViewDebugValue3);


            editTextIPAddress = FindViewById<EditText>(Resource.Id.editTextIPAddress);
            editTextIPPort = FindViewById<EditText>(Resource.Id.editTextIPPort);
            refreshTextInput = FindViewById<EditText>(Resource.Id.refreshTextInput);

            UpdateConnectionState(4, "Disconnected");

            // Init commandlist, scheduled by socket timer
            commandList.Add(new Tuple<string, TextView>("d", textViewChangePinStateValue1));
            commandList.Add(new Tuple<string, TextView>("e", textViewChangePinStateValue2));
            commandList.Add(new Tuple<string, TextView>("f", textViewChangePinStateValue3));
            commandList.Add(new Tuple<string, TextView>("g", textViewSensorValue1));
            //commandList.Add(new Tuple<string, TextView>("g", textViewSensorValue2));
            //commandList.Add(new Tuple<string, TextView>("g", textViewSensorValue3));

            this.Title = this.Title + " (timer sockets)";

            // timer object, running clock
            timerClock = new System.Timers.Timer() { Interval = 2000, Enabled = true }; // Interval >= 1000
            timerClock.Elapsed += (obj, args) =>
            {
                RunOnUiThread(() => { textViewTimerStateValue.Text = DateTime.Now.ToString("h:mm:ss"); }); 
            };

            // timer object, check Arduino state
            // Only one command can be serviced in an timer tick, schedule from list
            timerSockets = new System.Timers.Timer() { Interval = 1000, Enabled = false }; // Interval >= 750
            timerSockets.Elapsed += (obj, args) =>
            {
                //RunOnUiThread(() =>
                //{
                if (socket != null) // only if socket exists
                {
                    Log.Info("Domotica", Convert.ToString(listIndex));
                    // Send a command to the Arduino server on every tick (loop though list)
                    if (++listIndex >= commandList.Count)
                    {
                        listIndex = 0;
                    }
                    UpdateGUI(executeCommand(commandList[listIndex].Item1), commandList[listIndex].Item2);  //e.g. UpdateGUI(executeCommand("s"), textViewChangePinStateValue);
                }
                else
                {
                    timerSockets.Enabled = false;  // If socket broken -> disable timer
                }
                //});
            };

            //Add the "Connect" button handler.
            if (buttonConnect != null)  // if button exists
            {
                buttonConnect.Click += (sender, e) =>
                {
                    //Validate the user input (IP address and port)
                    if (CheckValidIpAddress(editTextIPAddress.Text) && CheckValidPort(editTextIPPort.Text))
                    {
                        ConnectSocket(editTextIPAddress.Text, editTextIPPort.Text);
                    }
                    else UpdateConnectionState(3, "Please check IP");
                };
            }

            //Add the "Change pin state 1" button handler.
            if (buttonChangePinState1 != null)
            {
                buttonChangePinState1.Click += (sender, e) =>
                {
                    socket.Send(Encoding.ASCII.GetBytes("a"));                 // Send toggle-command to the Arduino
                };
            }

            //Add the "Change pin state 2" button handler.
            if (buttonChangePinState2 != null)
            {
                buttonChangePinState2.Click += (sender, e) =>
                {
                    socket.Send(Encoding.ASCII.GetBytes("b"));                 // Send toggle-command to the Arduino
                };
            }

            //Add the "Change pin state 3" button handler.
            if (buttonChangePinState3 != null)
            {
                buttonChangePinState3.Click += (sender, e) =>
                {
                    socket.Send(Encoding.ASCII.GetBytes("c"));                 // Send toggle-command to the Arduino
                };
            }

            if (refreshButton != null)
            {
                refreshButton.Click += (sender, e) =>
                {
                    int frequency = 1000 / Convert.ToInt32(refreshTextInput.Text);
                    timerSockets.Interval = frequency;
                };
            }
        }


        //Send command to server and wait for response (blocking)
        //Method should only be called when socket existst
        public string executeCommand(string cmd)
        {
            byte[] buffer = new byte[4]; // response is always 4 bytes
            int bytesRead = 0;
            string result = "---";

            if (socket != null)
            {
                //Send command to server
                socket.Send(Encoding.ASCII.GetBytes(cmd));

                try //Get response from server
                {
                    //Store received bytes (always 4 bytes, ends with \n)
                    bytesRead = socket.Receive(buffer);  // If no data is available for reading, the Receive method will block until data is available,
                    //Read available bytes.              // socket.Available gets the amount of data that has been received from the network and is available to be read
                    while (socket.Available > 0) bytesRead = socket.Receive(buffer);
                    if (bytesRead == 4)
                        result = Encoding.ASCII.GetString(buffer, 0, bytesRead - 1); // skip \n
                    else result = "err";
                }
                catch (Exception exception) {
                    result = exception.ToString();
                    if (socket != null) {
                        socket.Close();
                        socket = null;
                    }
                    UpdateConnectionState(3, result);
                }
            }
            return result;
        }

        //Update connection state label (GUI).
        public void UpdateConnectionState(int state, string text)
        {
            // connectButton
            string butConText = "Connect";  // default text
            bool butConEnabled = true;      // default state
            Color color = Color.Red;        // default color
            // pinButton
            bool butPinEnabled = false;     // default state 

            //Set "Connect" button label according to connection state.
            if (state == 1)
            {
                butConText = "Please wait";
                color = Color.Orange;
                butConEnabled = false;
            } else
            if (state == 2)
            {
                butConText = "Disconnect";
                color = Color.Green;
                butPinEnabled = true;
            }
            //Edit the control's properties on the UI thread
            RunOnUiThread(() =>
            {
                textViewServerConnect.Text = text;
                if (butConText != null)  // text existst
                {
                    buttonConnect.Text = butConText;
                    textViewServerConnect.SetTextColor(color);
                    buttonConnect.Enabled = butConEnabled;
                }
                buttonChangePinState1.Enabled = butPinEnabled;
                buttonChangePinState2.Enabled = butPinEnabled;
                buttonChangePinState3.Enabled = butPinEnabled;
                refreshButton.Enabled = butPinEnabled;
            });
        }

        //Update GUI based on Arduino response
        public void UpdateGUI(string result, TextView textview)
        {
            RunOnUiThread(() =>
            {
                if(result == " O1")
                {
                    textview.SetTextColor(Color.Green);
                    textview.Text = "ON";
                }
                else if (result == "F-1")
                {
                    textview.SetTextColor(Color.Red);
                    textview.Text = "OFF";
                }
                else if (result == " O2")
                {
                    textview.SetTextColor(Color.Green);
                    textview.Text = "ON";
                }
                else if (result == "F-2")
                {
                    textview.SetTextColor(Color.Red);
                    textview.Text = "OFF";
                }
                else if (result == " O3")
                {
                    textview.SetTextColor(Color.Green);
                    textview.Text = "ON";
                }
                else if (result == "F-3")
                {
                    textview.SetTextColor(Color.Red);
                    textview.Text = "OFF";
                }
                else
                {
                    textview.SetTextColor(Color.White);
                    textview.Text = result;
                }
                //if socket.recieve == ON1 {}

                //if socket.recieve == 2 {}
                //if socket.recieve == 3 {}

                //if (result == "OFF") textview.SetTextColor(Color.Red);
                //else if (result == " ON") textview.SetTextColor(Color.Green);
                //else textview.SetTextColor(Color.White);  
                //textview.Text = result;
            });
        }

        // Connect to socket ip/prt (simple sockets)
        public void ConnectSocket(string ip, string prt)
        {
            RunOnUiThread(() =>
            {
                if (socket == null)                                       // create new socket
                {
                    UpdateConnectionState(1, "Connecting...");
                    try  // to connect to the server (Arduino).
                    {
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.Connect(new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(prt)));
                        if (socket.Connected)
                        {
                            UpdateConnectionState(2, "Connected");
                            timerSockets.Enabled = true;                //Activate timer for communication with Arduino     
                        }
                    } catch (Exception exception) {
                        timerSockets.Enabled = false;
                        if (socket != null)
                        {
                            socket.Close();
                            socket = null;
                        }
                        UpdateConnectionState(4, exception.Message);
                    }
	            }
                else // disconnect socket
                {
                    socket.Close(); socket = null;
                    timerSockets.Enabled = false;
                    UpdateConnectionState(4, "Disconnected");
                }
            });
        }

        //Close the connection (stop the threads) if the application stops.
        protected override void OnStop()
        {
            base.OnStop();
        }

        //Close the connection (stop the threads) if the application is destroyed.
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        //Prepare the Screen's standard options menu to be displayed.
        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            //Prevent menu items from being duplicated.
            menu.Clear();

            MenuInflater.Inflate(Resource.Menu.menu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        //Executes an action when a menu button is pressed.
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.exit:
                    //Force quit the application.
                    System.Environment.Exit(0);
                    return true;
                case Resource.Id.abort:
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        //Check if the entered IP address is valid.
        private bool CheckValidIpAddress(string ip)
        {
            if (ip != "") {
                //Check user input against regex (check if IP address is not empty).
                Regex regex = new Regex("\\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\\.|$)){4}\\b");
                Match match = regex.Match(ip);
                return match.Success;
            } else return false;
        }

        //Check if the entered port is valid.
        private bool CheckValidPort(string port)
        {
            //Check if a value is entered.
            if (port != "")
            {
                Regex regex = new Regex("[0-9]+");
                Match match = regex.Match(port);

                if (match.Success)
                {
                    int portAsInteger = Int32.Parse(port);
                    //Check if port is in range.
                    return ((portAsInteger >= 0) && (portAsInteger <= 65535));
                }
                else return false;
            } else return false;
        }
    }
}
