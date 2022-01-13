//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SpeechBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Media;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    // using System.Speech.Synthesis; 
    using Speech.Synthesis;
    using System.Net.Sockets;
    using System.Windows.Controls;
    using System.Timers;
    using System.Net;
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "In a full-fledged application, the SpeechRecognitionEngine object should be properly disposed. For the sake of simplicity, we're omitting that code in this sample.")]
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Resource key for medium-gray-colored brush.
        /// </summary>
        private const string MediumGreyBrushKey = "MediumGreyBrush";

        private static string Controler_IP = "10.237.100.3";

        static volatile Boolean unLocked = false;

        private Boolean inShutDownMode = false;

        private FileUtil myUtil = null;

        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private KinectAudioStream convertStream = null;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine = null;

        /// <summary>
        /// List of all UI span elements used to select recognized text.
        /// </summary>       
        //private List<Span> recognitionSpans;

        private SpeechSynthesizer _synthesizer = null;
        System.Media.SoundPlayer m_SoundPlayer = null; //  in case we want to keep the speeches in a file

        private volatile static Timer aTimer = null;
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            _synthesizer = new SpeechSynthesizer();
            // Configure the audio output. 
            // _synthesizer.SetOutputToWaveFile(@"C:\Users\bruce.gomes\Desktop\test.wav");
            try
            {
                _synthesizer.SetOutputToDefaultAudioDevice();
            }catch(InvalidOperationException e)
            {

                //_synthesizer.SetOutputToWaveFile(@"C:\Users\bruce.gomes\Desktop\test.wav");
                _synthesizer.SetOutputToWaveFile(@"Files\test.wav");
                //m_SoundPlayer = new System.Media.SoundPlayer(@"C:\Users\bruce.gomes\Desktop\test.wav");
                m_SoundPlayer = new System.Media.SoundPlayer(@"Files\test.wav");
            }
            _synthesizer.SelectVoice("Microsoft Server Speech Text to Speech Voice (en-US, ZiraPro)");

            // m_SoundPlayer = new System.Media.SoundPlayer(@"C:\Users\bruce.gomes\Desktop\test.wav")

            aTimer = new System.Timers.Timer(); // creates a separate thread to wait 5 secs till commands are accepted.
            myUtil = new FileUtil();
            startFile();
        }

        private void startFile()
        {
            String ip = myUtil.Read_File();
            bool valid = false;
            string input = ip;

            IPAddress address;
            if (IPAddress.TryParse(input, out address)) // checks ip from file validity
            {
                switch (address.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        // we have IPv4
                        valid = true;
                        break;
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        // we have IPv6
                        valid = true;
                        break;
                    default:
                        // umm... yeah... I'm going to need to take your red packet and...
                        valid = false;
                        break;
                }
            }

            if (valid)
            {
                Controler_IP = ip;
                this.ipBox.Text = Controler_IP;
            }
            else
                MessageBox.Show("The Current Stored IP Address is not valid.\nPlease type a new one and press save!", "Unknown Classroom IP Adress", MessageBoxButton.OK, MessageBoxImage.Error);
            

        }


        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;
            
            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        /// <summary>
        /// Execute initialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Only one sensor is supported
            this.kinectSensor = KinectSensor.GetDefault();
            if (this.kinectSensor != null)
            {
                // open the sensor
                this.kinectSensor.Open();

                // grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = this.kinectSensor.AudioSource.AudioBeams;
                System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

                // create the convert stream
                this.convertStream = new KinectAudioStream(audioStream);
            }
            else
            {
                // on failure, set the status text
                this.textBox.Text = this.textBox.Text + "\n" + Properties.Resources.NoKinectReady;
                return;
            }

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
             //   this.recognitionSpans = new List<Span> { forwardSpan, backSpan, rightSpan, leftSpan };

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                /****************************************************************
                * 
                * Use this code to create grammar programmatically rather than from
                * a grammar file.
                * 
                * var directions = new Choices();
                * directions.Add(new SemanticResultValue("forward", "FORWARD"));
                * directions.Add(new SemanticResultValue("forwards", "FORWARD"));
                * directions.Add(new SemanticResultValue("straight", "FORWARD"));
                * directions.Add(new SemanticResultValue("backward", "BACKWARD"));
                * directions.Add(new SemanticResultValue("backwards", "BACKWARD"));
                * directions.Add(new SemanticResultValue("back", "BACKWARD"));
                * directions.Add(new SemanticResultValue("turn left", "LEFT"));
                * directions.Add(new SemanticResultValue("turn right", "RIGHT"));
                *
                * var gb = new GrammarBuilder { Culture = ri.Culture };
                * gb.Append(directions);
                *
                * var g = new Grammar(gb);
                * 
                ****************************************************************/

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.SpeechGrammar)))
                {
                    var g = new Grammar(memoryStream);
                    this.speechEngine.LoadGrammar(g);
                }

                this.speechEngine.SpeechRecognized += this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected += this.SpeechRejected;
        
                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                this.textBox.Text = this.textBox.Text + "\n" + Properties.Resources.NoSpeechRecognizer;
            }
        }

        /// <summary>
        /// Execute un-initialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= this.SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }

            if (null != this.kinectSensor)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }

            aTimer.Close();
            aTimer.Dispose();
        }

    /*    /// <summary>
        /// Remove any highlighting from recognition instructions.
        /// </summary>
        private void ClearRecognitionHighlights()
        {
            foreach (Span span in this.recognitionSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];
                span.FontWeight = FontWeights.Normal;
            }
        }*/

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;
        //    this.ClearRecognitionHighlights();

            if (e.Result.Confidence >= ConfidenceThreshold)
            {

                switch (e.Result.Semantics.Value.ToString())
                {
                    /* case "REBOOT":
                         // telnet 10.237.100.2  pass: 1988
                         // Connect("reboot 10001:1:1", this.textBox);
                        break; */

                    case "OK ZIRA":
                        openLock();                      
                        break; 

                    case "WAKE":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvTP,'WAKE'", this.textBox))
                                _synthesizer.Speak("Waking");
                            else
                                _synthesizer.Speak("Waking failed");
                            //  m_SoundPlayer.Play();   in case of playing from a previously save speech file                    
                        }
                        break;

                    case "SLEEP":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvTP,'SLEEP'", this.textBox))
                                _synthesizer.Speak("sleeping");
                            else
                                _synthesizer.Speak("sleeping failed");
                        }
                         break;
                       
                    case "SYSTEM ON":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'systemon'", this.textBox))
                                _synthesizer.Speak("Turning on");
                            else
                                _synthesizer.Speak("powering on failed!!!");
                        }
                        break;

                    case "SHUTDOWN":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'shutdown'", this.textBox)) // mae new are you sure  case
                            {
                                _synthesizer.Speak("Are You sure you want to turn off ?");
                                this.inShutDownMode = true;
                            }
                            else
                            {
                                _synthesizer.Speak("powering off failed");
                                this.inShutDownMode = false;
                            }
                        }
                        break;

                    case "YES":
                        if (this.inShutDownMode == true)
                        {
                            if (Connect("send_command vdvKinect,'systemoff'", this.textBox))
                            {
                                this.inShutDownMode = false;
                                _synthesizer.Speak("Powering off");
                            }
                            else
                            {                               
                                SystemSounds.Hand.Play();
                                this.inShutDownMode = false;
                            }
                            Connect("off[vdvtp,4]", this.textBox);
                        }                      
                        break;

                    case "NO":
                    case "CANCEL":
                        if (this.inShutDownMode == true)
                        {
                            Connect("off[vdvtp,4]", this.textBox);
                            _synthesizer.Speak("Powering off cancelled.");
                            Connect("SEND_COMMAND vdvTP, '@PPX-Confirm_Exit'", this.textBox);
                            this.inShutDownMode = false;
                        }
                        break;
                                           
                    case "SHOW PC":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'pc'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "HELP":
                        if (unLocked == true)
                        {
                            if (Connect("SEND_COMMAND vdvTP, '@PPN-EMERGENCY'", this.textBox))
                            {
                                Connect("on[vdvtp,41]", this.textBox);
                                SystemSounds.Beep.Play();
                            }
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "BACK":
                        if (unLocked == true)
                        {
                            if (Connect("SEND_COMMAND vdvTP, '@PPX'", this.textBox))
                            {
                                Connect("off[vdvtp,41]", this.textBox);
                                SystemSounds.Beep.Play();
                            }
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "SHOW DOCCAM":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'doccam'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "SHOW DVD":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'dvd/vcr'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "SHOW LAPTOP":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'laptop'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "START CONFERENCE":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'start_conference'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "EXIT CONFERENCE":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'exit_conference'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "VOLUME UP":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'volume_up'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "VOLUME DOWN":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'volume_down'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "MUTE":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'mute/unmute'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "UNMUTE":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'mute/unmute'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "CAMERA CONTROL":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'camera_control'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "STUDENT CAMERA":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'student_camera'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "INSTRUCTOR CAMERA":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'instructor_camera'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "ZOOM IN":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'zoom_in'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "ZOOM OUT":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'zoom_out'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "PAN LEFT":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'pan_left'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "PAN RIGHT":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'pan_right'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "PAN UP":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'pan_up'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                    case "PAN DOWN":
                        if (unLocked == true)
                        {
                            if (Connect("send_command vdvKinect,'pan_down'", this.textBox))
                                SystemSounds.Beep.Play();
                            else
                                SystemSounds.Hand.Play();
                        }
                        break;

                 } // end switch
            } 
        }

        private void openLock()
        {
            
            SystemSounds.Beep.Play();
            if (unLocked == false)
            {
                unLocked = true;
                //System.Timers.Timer aTimer = new System.Timers.Timer();
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                aTimer.Interval = 5000;
                aTimer.AutoReset = false;
                aTimer.Start();
            }
            
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            unLocked = false;
            aTimer.Stop();
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
          //  this.ClearRecognitionHighlights();
        }

        static Boolean Connect(String command, TextBox txtBox)
        {
            Boolean res;
               
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                Int32 port = 23;
                System.Net.Sockets.TcpClient client = null;
                try
                {
                    client = new TcpClient(Controler_IP, port);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("Cannot estabilish a connection with current ip: " + Controler_IP, "Invalid IP Address", MessageBoxButton.OK, MessageBoxImage.Hand);
                }
                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(command + "\r\n");

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);

                //Console.WriteLine("Sent: {0}", message);
                txtBox.Text = "Sent: " + command.ToString() + "\n";
                // Receive the TcpServer.response.

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                // Console.WriteLine("Received: {0}", responseData);
                // txtBox.Text += "Received: " + responseData + "\n";

                // Close everything.
                stream.Close();
                client.Close();
                res = true;
            }
            catch (ArgumentNullException e)
            {
                // Console.WriteLine("ArgumentNullException: {0}", e);
                txtBox.Text += "ArgumentNullException: " + e + "\n";
                res = false;
            }
            catch (SocketException e)
            {
               //Console.WriteLine("SocketException: {0}", e);
                txtBox.Text += "SocketException: " + e + "\n";
                res = false;
            }

            // Console.WriteLine("\n Press Enter to continue...");
            // Console.Read();
            return res;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Controler_IP = this.ipBox.Text;
            Controler_IP.Trim();
            this.myUtil.writeToFile(Controler_IP);
            this.ipBox.IsEnabled = false;
            this.saveButton.IsEnabled = false;
            this.resetButton.IsEnabled = true;

        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            this.ipBox.Text = String.Empty;
            this.ipBox.IsEnabled = true;
            this.saveButton.IsEnabled = true;
            this.resetButton.IsEnabled = false;
            Controler_IP = string.Empty;
        }
    }
}