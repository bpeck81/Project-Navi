using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Collections.ObjectModel;

namespace KinectFaceFollower
{
    class FaceIdenitifier
    {
        private FaceServiceClient faceServiceClient;
        string groupName; // only works with number?
        public bool dbIsValid { get; set; }
        public FaceIdenitifier()
        {
            dbIsValid = false;
            groupName = "2";
            faceServiceClient = new FaceServiceClient("ef3c8df5f4ef405d8e493691d4315d11");
        }
        public async void CreateDatabase()
        {

            try
            {
                await faceServiceClient.DeletePersonGroupAsync(groupName);
            }
                catch (FaceAPIException ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                await faceServiceClient.CreatePersonGroupAsync(groupName, groupName);
            }
            catch (FaceAPIException ex)
            {
                Console.WriteLine("not made");
                Console.WriteLine(ex.Message);
            }
            var filePath = "C:/Users/brand_000/Pictures/Camera Roll";
            var personList = new ObservableCollection<Person>();
            try
            {
                int count = 0;
                foreach (var dir in Directory.EnumerateDirectories(filePath))
                {
                    count++;

                    var tag = System.IO.Path.GetFileName(dir);

                    Person person = new Person();
                    person.Name = tag;
                    try
                    {
                        person.PersonId = (await faceServiceClient.CreatePersonAsync(groupName, person.Name)).PersonId;
                        foreach (string imagePath in Directory.GetFiles(dir, "*.jpg"))
                        {
                            using (Stream s = File.OpenRead(imagePath))
                            {
                                try
                                {
                                    await faceServiceClient.AddPersonFaceAsync(
                                        groupName, person.PersonId, s);
                                }
                                catch (FaceAPIException ex)
                                {
                                    Console.WriteLine(ex.ErrorMessage);
                                }
                            }
                        }
                    }
                    catch (FaceAPIException ex)
                    {
                        Console.WriteLine(ex.ErrorCode);
                        Console.WriteLine(ex.ErrorMessage);
                    }

                }
                await faceServiceClient.TrainPersonGroupAsync(groupName);

            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }

            dbIsValid = true;

        }

        public async Task<Tuple<FaceRectangle[], List<string>>> IdentifyWithImage(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    Console.WriteLine(faces.Length);
                    var faceRects = faces.Select(face => face.FaceRectangle).ToArray();
                    IdentifyResult[] results = null;
                    try
                    {
                         results = await faceServiceClient.IdentifyAsync(groupName, faces.Select(ff => ff.FaceId).ToArray());

                    }
                    catch(FaceAPIException ex)
                    {
                        Console.WriteLine(ex.ErrorMessage);
                    }


                        List<string> identifiedNames = new List<string>();
                    foreach (var identifyResult in results)
                    {

                        if (identifyResult.Candidates.Length != 0)
                        {
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            var person = await faceServiceClient.GetPersonAsync(groupName, candidateId);
                            Console.WriteLine("Identified as {0}", person.Name);
                            identifiedNames.Add(person.Name);

                        }
                        else
                        {
                            identifiedNames.Add("Unknown");
                        }
                    }

                    //returns face rectangles with names at corresponding index values
                    return new Tuple<Microsoft.ProjectOxford.Face.Contract.FaceRectangle[], List<string>>(faceRects.ToArray(), identifiedNames);
                }
            }
            catch (Exception)
            {
                return null;

            }
        }
    }
}
