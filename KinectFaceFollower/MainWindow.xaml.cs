using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Speech;
using Microsoft.Kinect.Face;
using System.Globalization;
using System.IO;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Speech.Synthesis;
using Microsoft.Speech.AudioFormat;
using System.Threading;
using Twilio;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace KinectFaceFollower
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        ColorFrameReader colorReader;
        BodyFrameReader bodyReader;
        IList<Body> bodies;
        FaceFrameSource faceSource;
        FaceFrameReader faceReader;
        public static WriteableBitmap currentFrameBitmap;
        FaceIdenitifier faceIdentifier;
        SpeechSynthesizer synth;
        string savePhotoPath;
        int iterFaceSeen; //used to take only one photo
        int colorFrameCount;
        Thread analyzeFaceThread;
        bool allowedIn;

        public MainWindow()
        {
            InitializeComponent();
            synth = new SpeechSynthesizer();
            synth.Volume = 100;
            allowedIn = false;
            synth.SelectVoiceByHints(VoiceGender.Female);
            synth.Rate = 1;
            savePhotoPath = null;
            currentFrameBitmap = null;
            iterFaceSeen = 0;
            colorFrameCount = 0;
            sensor = KinectSensor.GetDefault();
            faceIdentifier = new FaceIdenitifier();
            if(sensor != null)
            {
                sensor.Open();
                bodies = new Body[sensor.BodyFrameSource.BodyCount];
                colorReader = sensor.ColorFrameSource.OpenReader();
                colorReader.FrameArrived += ColorReader_FrameArrived;
                bodyReader = sensor.BodyFrameSource.OpenReader();
                bodyReader.FrameArrived += BodyReader_FrameArrived;
                faceSource = new FaceFrameSource(sensor, 0, FaceFrameFeatures.BoundingBoxInColorSpace |
                    FaceFrameFeatures.FaceEngagement |
                    FaceFrameFeatures.Glasses |
                    FaceFrameFeatures.Happy |
                    FaceFrameFeatures.LeftEyeClosed |
                    FaceFrameFeatures.MouthOpen |
                    FaceFrameFeatures.PointsInColorSpace |
                    FaceFrameFeatures.RightEyeClosed);
                faceReader = faceSource.OpenReader();
                faceReader.FrameArrived += FaceReader_FrameArrived;
            }
        }

        private async void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            Tuple<FaceRectangle[], List<string>> rectsAndNames = null;
            using(var frame = e.FrameReference.AcquireFrame())
            {
                if(frame!= null)
                {
                    var result = frame.FaceFrameResult;
            
                    if(result != null)
                    {
                        iterFaceSeen++;

                        if (iterFaceSeen == 2)
                        {
                       

                            if (faceIdentifier.dbIsValid)
                            {
                                takeStorePhoto();

                                rectsAndNames = await faceIdentifier.IdentifyWithImage(savePhotoPath);
                            }
                            else
                            {
                                iterFaceSeen = 0;
                            }
                            if(rectsAndNames != null)
                            {
                                string name = rectsAndNames.Item2[0];
                                if (name.Equals("Unknown")) //ask permission
                                {
                                    synth.SpeakAsync("Hi, I don't recognize you. I'm going to tell someone inside that you're out here");

                                }
                                else //let inside
                                {
                                    synth.SpeakAsync("Whats up " + name + " great to see you today. Hold on a second while I talk to someone inside.");

                                }
                                allowedIn = Client.callDB(name);
                                if (allowedIn)
                                {
                                    synth.Speak("Everything is set. Come on in!");
                                }
                                else
                                {
                                    synth.Speak("I'm sorry but I cant let you in. I suggest you leave");

                                }
                               
                                sendTextMessage(name);

                            }
                        }

                        var eyeLeft = result.FacePointsInColorSpace[FacePointType.EyeLeft];
                        var eyeRight = result.FacePointsInColorSpace[FacePointType.EyeRight];
                        var nose = result.FacePointsInColorSpace[FacePointType.Nose];
                        var mouthLeft = result.FacePointsInColorSpace[FacePointType.MouthCornerLeft];
                        var mouthRight = result.FacePointsInColorSpace[FacePointType.MouthCornerRight];

                        var eyeLeftClosed = result.FaceProperties[FaceProperty.LeftEyeClosed];
                        var eyeRightClosed = result.FaceProperties[FaceProperty.RightEyeClosed];
                        var mouthOpen = result.FaceProperties[FaceProperty.MouthOpen];
                        var boundingBox = result.FaceBoundingBoxInColorSpace;
                        
                        
                        Canvas.SetLeft(rectangleHead,boundingBox.Left);
                        Canvas.SetTop(rectangleHead, boundingBox.Top);
                        rectangleHead.Width = Math.Abs(boundingBox.Left - boundingBox.Right);
                        rectangleHead.Height = Math.Abs(boundingBox.Top - boundingBox.Bottom);
                        /*
                        Canvas.SetLeft(ellipseEyeLeft, eyeLeft.X - ellipseEyeLeft.Width / 2.0);
                        Canvas.SetTop(ellipseEyeLeft, eyeLeft.Y - ellipseEyeLeft.Height / 2.0);

                        Canvas.SetLeft(ellipseEyeRight, eyeRight.X - ellipseEyeRight.Width / 2.0);
                        Canvas.SetTop(ellipseEyeRight, eyeRight.Y - ellipseEyeRight.Height / 2.0);

                        Canvas.SetLeft(ellipseNose, nose.X - ellipseNose.Width / 2.0);
                        Canvas.SetTop(ellipseNose, nose.Y - ellipseNose.Height / 2.0);

                        Canvas.SetLeft(ellipseMouth, ((mouthRight.X + mouthLeft.X) / 2.0) - ellipseMouth.Width / 2.0);
                        Canvas.SetTop(ellipseMouth, ((mouthRight.Y + mouthLeft.Y) / 2.0) - ellipseMouth.Height / 2.0);
                        ellipseMouth.Width = Math.Abs(mouthRight.X - mouthLeft.X);

                        if(eyeLeftClosed == DetectionResult.Yes || eyeLeftClosed == DetectionResult.Maybe)
                        {
                            ellipseEyeLeft.Visibility = Visibility.Collapsed;
            
                        }
                        else
                        {
                            ellipseEyeLeft.Visibility = Visibility.Visible;
                        }
                        if (eyeRightClosed == DetectionResult.Yes || eyeRightClosed == DetectionResult.Maybe)
                        {
                            ellipseEyeRight.Visibility = Visibility.Collapsed;

                        }
                        else
                        {
                            ellipseEyeRight.Visibility = Visibility.Visible;
                        }
                        if (mouthOpen == DetectionResult.Yes || mouthOpen == DetectionResult.Maybe)
                        {
                            
                            ellipseMouth.Height = 50.0;

                            
                        }
                        else {
                            ellipseEyeRight.Height = 20.0;
                        }
                        */

                    }
                }
            }

        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using(var frame = e.FrameReference.AcquireFrame())
            {
                if(frame != null)
                {
                    frame.GetAndRefreshBodyData(bodies);
                    var body = bodies.Where(b => b.IsTracked).FirstOrDefault();
                    if (!faceSource.IsTrackingIdValid)
                    {
                        if(body!= null)
                        {
                            faceSource.TrackingId = body.TrackingId; //link body to face
                        }
                    }
                }
            }
            
        }

        private void ColorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if(colorFrameCount == 1) //setup database at startup
                    {

                        sendTextMessage("Brandon");

                        faceIdentifier.CreateDatabase();
                    }
                    currentFrameBitmap = frame.ToBitmap();

                    camera.Source = currentFrameBitmap;
                    colorFrameCount++;
                }
            }

        }

        private void takeStorePhoto()
        {
            if(currentFrameBitmap != null)
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(currentFrameBitmap));
                var myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                savePhotoPath = System.IO.Path.Combine(myPhotos, "ProjectNavi/KinectFaceScreenshot.jpg");

                try
                {
                    using(FileStream fs = new FileStream(savePhotoPath, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("IO Exception!");
                }
            }
           
        }

        private void sendTextMessage(string friendName)
        {
            var blobCredentials = new StorageCredentials("michaelmwhite", "NKjJe50Q6BN599Bw6bOR5x0rH62aiK5hUMhqvtmcyRYvzrwOmK9mGKCMbawJ3MSZIUFt2YOPuvkyzgVti0Oa9w==");
            var blobClient = new CloudBlobClient(new Uri("https://michaelmwhite.blob.core.windows.net/containername"), blobCredentials);
            var container = blobClient.GetContainerReference("containerName");
            var blobSaveName = "KinectFaceScreenshot.jpg";
            var blockBlob = container.GetBlockBlobReference(String.Format("{0}", blobSaveName));

            using (var fileStream = System.IO.File.OpenRead("C:/Users/brand_000/Pictures/ProjectNavi/" + blobSaveName)) 
            {
                blockBlob.UploadFromStream(fileStream);
            }



            string accountSid = "AC7396599caf276f955026db453012bef3";
            string authToken = "3a9c8fa7c5f72b20e7c5a4b28090fd03";
            var client = new TwilioRestClient(accountSid, authToken);
            if (friendName.Equals("Unknown"))
            {
                friendName = "Someone I don't recognize";
            }
            var message = client.SendMessage("2404122858", "5712513711", friendName + " is outside!!", new string[] { "https://michaelmwhite.blob.core.windows.net/containername/containerName/" + blobSaveName } );
        }
    }
}
