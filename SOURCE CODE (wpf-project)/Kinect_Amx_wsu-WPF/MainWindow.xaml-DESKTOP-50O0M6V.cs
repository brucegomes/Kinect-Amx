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
    using Microsoft.Speech.Synthesis;
    using System.Net.Sockets;
    using System.Windows.Controls;

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

        private const string Controler_IP = "10.237.100.3";

        private Boolean inShutDownMode = false;

        /// <summary>
        /// Map between each direction and the direction immediately to its right.
        /// </summary>
        private static readonly Dictionary<Direction, Direction> TurnRight = new Dictionary<Direction, Direction>
            {
                { Direction.Up, Direction.Right },
                { Direction.Right, Direction.Down },
                { Direction.Down, Direction.Left },
                { Direction.Left, Direction.Up }
            };

        /// <summary>
        /// Map between each direction and the direction immediately to its left.
        /// </summary>
        private static readonly Dictionary<Direction, Direction> TurnLeft = new Dictionary<Direction, Direction>
            {
                { Direction.Up, Direction.Left },
                { Direction.Right, Direction.Up },
                { Direction.Down, Direction.Right },
                { Direction.Left, Direction.Down }
            };

        /// <summary>
        /// Map between each direction and the displacement unit it represents.
        /// </summary>
        private static readonly Dictionary<Direction, Point> Displacements = new Dictionary<Direction, Point>
            {
                { Direction.Up, new Point { X = 0, Y = -1 } },
                { Direction.Right, new Point { X = 1, Y = 0 } },
                { Direction.Down, new Point { X = 0, Y = 1 } },
                { Direction.Left, new Point { X = -1, Y = 0 } }
            };

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
        /// Current direction where turtle is facing.
        /// </summary>
        private Direction curDirection = Direction.Up;

        /// <summary>
        /// List of all UI span elements used to select recognized text.
        /// </summary>
        private List<Span> recognitionSpans;

        private SpeechSynthesizer _synthesizer = null;
       // System.Media.SoundPlayer m_SoundPlayer = null;  in case we want to keep the speeches in a file


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            _synthesizer = new SpeechSynthesizer();
            // Configure the audio output. 
            // _synthesizer.SetOutputToWaveFile(@"C:\Users\bruce.gomes\Desktop\test.wav");

            _synthesizer.SetOutputToDefaultAudioDevice();
            _synthesizer.SelectVoice("Microsoft Server Speech Text to Speech Voice (en-US, ZiraPro)");

           // m_SoundPlayer = new System.Media.SoundPlayer(@"C:\Users\bruce.gomes\Desktop\test.wav")
        }

        /// <summary>
        /// Enumeration of directions in which turtle may be facing.
        /// </summary>
        private enum Direction
        {
            /// <summary>
            /// Represents going up
            /// </summary>
            Up,

            /// <summary>
            /// Represents going down
            /// </summary>
            Down,

            /// <summary>
            /// Represents going left
            /// </summary>
            Left,

            /// <summary>
            /// Represents going right
            /// </summary>
            Right
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
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
                return;
            }

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
                this.recognitionSpans = new List<Span> { forwardSpan, backSpan, rightSpan, leftSpan };

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
                this.statusBarText.Text = Properties.Resources.NoSpeechRecognizer;
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
        }

        /// <summary>
        /// Remove any highlighting from recognition instructions.
        /// </summary>
        private void ClearRecognitionHighlights()
        {
            foreach (Span span in this.recognitionSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];
                span.FontWeight = FontWeights.Normal;
            }
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            // Number of degrees in a right angle.
            const int DegreesInRightAngle = 90;

            // Number of pixels turtle should move forwards or backwards each time.
            const int DisplacementAmount = 60;

            this.ClearRecognitionHighlights();

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "REBOOT":
                        // telnet 10.237.100.2  pass: 1988
                       // Connect("reboot 10001:1:1", this.textBox);
                        break;

                    case "WAKE":                    
                        if(Connect("send_command vdvTP,'WAKE'", this.textBox))
                            _synthesizer.Speak("Waking");
                        else
                            _synthesizer.Speak("Waking failed");
                        //  m_SoundPlayer.Play();   in case of playing from a previously save speech file                    
                        break;

                    case "SLEEP":
                         if(Connect("send_command vdvTP,'SLEEP'", this.textBox))
                            _synthesizer.Speak("sleeping");
                         else
                            _synthesizer.Speak("sleeping failed");
                         break;
                       
                    case "SYSTEM ON":
                        if(Connect("send_command vdvKinect,'systemon'", this.textBox))
                            _synthesizer.Speak("Turning on");
                        else
                            _synthesizer.Speak("powering on failed!!!");
                        break;

                    case "SHUTDOWN":
                        if (Connect("send_command vdvKinect,'shutdown'", this.textBox)) // mae new are you sure  case
                        {
                            _synthesizer.Speak("Are You sure you want to shutdown ?");
                            this.inShutDownMode = true;
                        }
                        else
                        {
                            _synthesizer.Speak("powering off failed");
                            this.inShutDownMode = false;
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
                            _synthesizer.Speak("Shutdown cancelled.");
                            Connect("SEND_COMMAND vdvTP, '@PPX-Confirm_Exit'", this.textBox);
                            this.inShutDownMode = false;
                        }
                        break;

                    case "SHOW PC":
                        if (Connect("send_command vdvKinect,'pc'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "SHOW DOCCAM":
                        if (Connect("send_command vdvKinect,'doccam'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "SHOW DVD":
                        if (Connect("send_command vdvKinect,'dvd/vcr'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "SHOW LAPTOP":
                        if (Connect("send_command vdvKinect,'laptop'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "START CONFERENCE":
                        if (Connect("send_command vdvKinect,'start_conference'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "EXIT CONFERENCE":
                        if (Connect("send_command vdvKinect,'exit_conference'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "VOLUME UP":
                        if(Connect("send_command vdvKinect,'volume_up'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "VOLUME DOWN":
                        if (Connect("send_command vdvKinect,'volume_down'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "MUTE":
                        if(Connect("send_command vdvKinect,'mute/unmute'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "UNMUTE":
                        if(Connect("send_command vdvKinect,'mute/unmute'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "CAMERA CONTROL":
                        if (Connect("send_command vdvKinect,'camera_control'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "STUDENT CAMERA":
                        if (Connect("send_command vdvKinect,'student_camera'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "INSTRUCTOR CAMERA":
                        if (Connect("send_command vdvKinect,'instructor_camera'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "ZOOM IN":
                        if (Connect("send_command vdvKinect,'zoom_in'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "ZOOM OUT":
                        if (Connect("send_command vdvKinect,'zoom_out'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "PAN LEFT":
                        if (Connect("send_command vdvKinect,'pan_left'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "PAN RIGHT":
                        if (Connect("send_command vdvKinect,'pan_right'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "PAN UP":
                        if (Connect("send_command vdvKinect,'pan_up'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;

                    case "PAN DOWN":
                        if (Connect("send_command vdvKinect,'pan_down'", this.textBox))
                            SystemSounds.Beep.Play();
                        else
                            SystemSounds.Hand.Play();
                        break;
                        
                    case "FORWARD":
                        forwardSpan.Foreground = Brushes.DeepSkyBlue;
                        forwardSpan.FontWeight = FontWeights.Bold;
                        turtleTranslation.X = (playArea.Width + turtleTranslation.X + (DisplacementAmount * Displacements[this.curDirection].X)) % playArea.Width;
                        turtleTranslation.Y = (playArea.Height + turtleTranslation.Y + (DisplacementAmount * Displacements[this.curDirection].Y)) % playArea.Height;
                        break;

                    case "BACKWARD":
                        backSpan.Foreground = Brushes.DeepSkyBlue;
                        backSpan.FontWeight = FontWeights.Bold;
                        turtleTranslation.X = (playArea.Width + turtleTranslation.X - (DisplacementAmount * Displacements[this.curDirection].X)) % playArea.Width;
                        turtleTranslation.Y = (playArea.Height + turtleTranslation.Y - (DisplacementAmount * Displacements[this.curDirection].Y)) % playArea.Height;
                        break;

                    case "LEFT":
                        leftSpan.Foreground = Brushes.DeepSkyBlue;
                        leftSpan.FontWeight = FontWeights.Bold;
                        this.curDirection = TurnLeft[this.curDirection];

                        // We take a left turn to mean a counter-clockwise right angle rotation for the displayed turtle.
                        turtleRotation.Angle -= DegreesInRightAngle;
                        break;

                    case "RIGHT":
                        rightSpan.Foreground = Brushes.DeepSkyBlue;
                        rightSpan.FontWeight = FontWeights.Bold;
                        this.curDirection = TurnRight[this.curDirection];
                        // We take a right turn to mean a clockwise right angle rotation for the displayed turtle.
                        turtleRotation.Angle += DegreesInRightAngle;
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.ClearRecognitionHighlights();
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
                System.Net.Sockets.TcpClient client = new TcpClient(Controler_IP, port);

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
                //Console.WriteLine("Received: {0}", responseData);
             //   txtBox.Text += "Received: " + responseData + "\n";

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
    }
}