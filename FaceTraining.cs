using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace face_quickstart
{
    class Program
    {
        static string sourcePersonGroup = null;
        const string IMAGE_BASE_URL = "https://csdx.blob.core.windows.net/resources/Face/Images/";
        const string RECOGNITION_MODEL1 = RecognitionModel.Recognition01;
        const string RECOGNITION_MODEL2 = RecognitionModel.Recognition02;

        string meme = "f0887a48-8afc-4ed0-bc41-ba1f664b9d98";

        static void Main(string[] args)
        {
            string SUBSCRIPTION_KEY = "193d3a91e1d842beb51672a4da3c4ca1";
            string ENDPOINT = "https://utvecklingsprojekt.cognitiveservices.azure.com/";


            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);



            //DetectFaceExtract(client, "https://halsoteknikcentrum.hh.se/wp-content/uploads/2018/02/Wagner2.jpg", RECOGNITION_MODEL2).Wait();
            createGroup(client, IMAGE_BASE_URL, RECOGNITION_MODEL1).Wait();
            //recognizeImage(client, IMAGE_BASE_URL, RECOGNITION_MODEL1, "identification1.jpg", "f0887a48-8afc-4ed0-bc41-ba1f664b9d98").Wait();
        }

        public static async Task recognizeImage(IFaceClient client, string url, string recognitionModel, string sourceImageFileName, string personGroupId) {
            List<Guid> sourceFaceIds = new List<Guid>();
            // Detect faces from source image url.
            List<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{url}{sourceImageFileName}", recognitionModel);

            // Add detected faceId to sourceFaceIds.
            foreach (var detectedFace in detectedFaces) {
                sourceFaceIds.Add(detectedFace.FaceId.Value);
            }

            // Identify the faces in a person group. 
            var identifyResults = await client.Face.IdentifyAsync(sourceFaceIds, personGroupId);

            foreach (var identifyResult in identifyResults)
            {
                Person person = await client.PersonGroupPerson.GetAsync(personGroupId, identifyResult.Candidates[0].PersonId);
                Console.WriteLine($"Person '{person.Name}' is identified for face in: {sourceImageFileName} - {identifyResult.FaceId}," +
                    $" confidence: {identifyResult.Candidates[0].Confidence}.");
            }
            Console.WriteLine();
        }

        public static async Task createGroup(IFaceClient client, string url, string recognitionModel) {
            // Create a dictionary for all your images, grouping similar ones under the same key.
            Dictionary<string, string[]> personDictionary =
                new Dictionary<string, string[]>
                    {
                        { "Wagner", new[] { "https://halsoteknikcentrum.hh.se/wp-content/uploads/2018/02/Wagner2.jpg", "http://api.staff.hh.se/getImage/618F85BB-31C3-4254-8609-FA700E38E8BB/l", "https://softwareday.lindholmen.se/sites/default/files/content/styles/resource_publication_operator/public/content/resource/previews/wagner.png?itok=6o3i4njO" } },
                        { "Kalle" , new[] { "https://i.imgur.com/efj2Wb8.jpg", "https://i.imgur.com/eQFwhT8.jpg", "https://i.imgur.com/J29Wdmh.jpg", "https://i.imgur.com/vxSuZ2f.jpg", "https://i.imgur.com/Xs0fJjM.jpg", "https://i.imgur.com/TJ517Ro.jpg", "https://i.imgur.com/X41wj7L.jpg", "https://i.imgur.com/UYGSP31.jpg" } },
                        { "Hans-Erik", new [] {"https://i.imgur.com/4ULKpqI.jpg", "https://i.imgur.com/K50asCU.jpg", "https://i.imgur.com/Zf05sIU.jpg", "https://i.imgur.com/pQdVCeH.jpg", "https://i.imgur.com/NGwmMnz.jpg", "https://i.imgur.com/wqHuivs.jpg", "https://i.imgur.com/li896Kr.jpg", "https://i.imgur.com/aHX7pP6.jpg" } },
                        { "Pontus", new [] {"https://i.imgur.com/Lv0FfaB.jpg", "https://i.imgur.com/tGJNh5W.jpg", "https://i.imgur.com/Gg5PwvN.jpg", "https://i.imgur.com/HCiKe8f.jpg", "https://i.imgur.com/esd2sxy.jpg", "https://i.imgur.com/fhEHjNh.jpg", "https://i.imgur.com/RR7a9uE.jpg", "https://i.imgur.com/27PVPVU.jpg", "https://i.imgur.com/BIdztuF.jpg", "https://i.imgur.com/0CpnJbq.jpg"}},
                        { "Joacim", new [] {"https://i.imgur.com/VYMiEPt.jpg", "https://i.imgur.com/ByYNxh5.jpg", "https://i.imgur.com/tzMGm3s.jpg", "https://i.imgur.com/zUCtqnx.jpg", "https://i.imgur.com/MJH4VGL.jpg", "https://i.imgur.com/3xqLb7O.jpg", "https://i.imgur.com/3QIZeyN.jpg", "https://i.imgur.com/DxK1Y4s.jpg", "https://i.imgur.com/XAoqEWd.jpg", "https://i.imgur.com/MIFLIQl.jpg"}},
                        { "Marcus", new [] {"https://i.imgur.com/4a6Fj9L.jpg", "https://i.imgur.com/ndxNbSn.jpg", "https://i.imgur.com/A3pcatZ.jpg", "https://i.imgur.com/zEXdwpT.jpg", "https://i.imgur.com/CRIn9R6.jpg", "https://i.imgur.com/JLwYQhG.jpg", "https://i.imgur.com/I5SMXDm.jpg", "https://i.imgur.com/oRR59JD.jpg"}}
                    } ;
            // A group photo that includes some of the persons you seek to identify from your dictionary.
            string sourceImageFileName = "identification1.jpg";

            // Create a person group. 
            string personGroupId = Guid.NewGuid().ToString();
            sourcePersonGroup = personGroupId; // This is solely for the snapshot operations example
            Console.WriteLine($"Create a person group ({personGroupId}).");
            await client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: recognitionModel);
            // The similar faces will be grouped into a single person group person.
            foreach (var groupedFace in personDictionary.Keys)
            {
                // Limit TPS
                await Task.Delay(250);
                Person person = await client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: groupedFace);
                Console.WriteLine($"Create a person group person '{groupedFace}'.");

                // Add face to the person group person.
                foreach (var similarImage in personDictionary[groupedFace])
                {
                    Console.WriteLine($"Add face to the person group person({groupedFace}) from image `{similarImage}`");
                    PersistedFace face = await client.PersonGroupPerson.AddFaceFromUrlAsync(personGroupId, person.PersonId,
                        $"{similarImage}", similarImage);
                }
            }

            // Start to train the person group.
            Console.WriteLine();
            Console.WriteLine($"Train person group {personGroupId}.");
            await client.PersonGroup.TrainAsync(personGroupId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.PersonGroup.GetTrainingStatusAsync(personGroupId);
                Console.WriteLine($"Training status: {trainingStatus.Status}.");
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }
        }



        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string RECOGNITION_MODEL1)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(url, recognitionModel: RECOGNITION_MODEL1);
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(url)}`");
            return detectedFaces.ToList();
        }


        /* 
 * DETECT FACES
 * Detects features from faces and IDs them.
 */
        public static async Task DetectFaceExtract(IFaceClient client, string url, string recognitionModel)
        {
            Console.WriteLine("========DETECT FACES========");
            Console.WriteLine();

            
                // Detect faces with all attributes from image url.
                IList<DetectedFace> detectedFaces = await client.Face.DetectWithUrlAsync(url,
                        returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.Accessories, FaceAttributeType.Age,
                FaceAttributeType.Blur, FaceAttributeType.Emotion, FaceAttributeType.Exposure, FaceAttributeType.FacialHair,
                FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair, FaceAttributeType.HeadPose,
                FaceAttributeType.Makeup, FaceAttributeType.Noise, FaceAttributeType.Occlusion, FaceAttributeType.Smile },
                        recognitionModel: recognitionModel);

                Console.WriteLine($"{detectedFaces.Count} face(s) detected.");


                var face = detectedFaces.First();

                // Parse and print all attributes of each detected face.
                
                    Console.WriteLine($"Face attributes for the detected face:");

                    // Get bounding box of the faces
                    Console.WriteLine($"Rectangle(Left/Top/Width/Height) : {face.FaceRectangle.Left} {face.FaceRectangle.Top} {face.FaceRectangle.Width} {face.FaceRectangle.Height}");

                    // Get accessories of the faces
                    List<Accessory> accessoriesList = (List<Accessory>)face.FaceAttributes.Accessories;
                    int count = face.FaceAttributes.Accessories.Count;
                    string accessory; string[] accessoryArray = new string[count];
                    if (count == 0) { accessory = "NoAccessories"; }
                    else
                    {
                        for (int i = 0; i < count; ++i) { accessoryArray[i] = accessoriesList[i].Type.ToString(); }
                        accessory = string.Join(",", accessoryArray);
                    }
                    Console.WriteLine($"Accessories : {accessory}");

                    // Get face other attributes
                    Console.WriteLine($"Age : {face.FaceAttributes.Age}");
                    Console.WriteLine($"Blur : {face.FaceAttributes.Blur.BlurLevel}");

                    // Get emotion on the face
                    string emotionType = string.Empty;
                    double emotionValue = 0.0;
                    Emotion emotion = face.FaceAttributes.Emotion;
                    if (emotion.Anger > emotionValue) { emotionValue = emotion.Anger; emotionType = "Anger"; }
                    if (emotion.Contempt > emotionValue) { emotionValue = emotion.Contempt; emotionType = "Contempt"; }
                    if (emotion.Disgust > emotionValue) { emotionValue = emotion.Disgust; emotionType = "Disgust"; }
                    if (emotion.Fear > emotionValue) { emotionValue = emotion.Fear; emotionType = "Fear"; }
                    if (emotion.Happiness > emotionValue) { emotionValue = emotion.Happiness; emotionType = "Happiness"; }
                    if (emotion.Neutral > emotionValue) { emotionValue = emotion.Neutral; emotionType = "Neutral"; }
                    if (emotion.Sadness > emotionValue) { emotionValue = emotion.Sadness; emotionType = "Sadness"; }
                    if (emotion.Surprise > emotionValue) { emotionType = "Surprise"; }
                    Console.WriteLine($"Emotion : {emotionType}");

                    // Get more face attributes
                    Console.WriteLine($"Exposure : {face.FaceAttributes.Exposure.ExposureLevel}");
                    Console.WriteLine($"FacialHair : {string.Format("{0}", face.FaceAttributes.FacialHair.Moustache + face.FaceAttributes.FacialHair.Beard + face.FaceAttributes.FacialHair.Sideburns > 0 ? "Yes" : "No")}");
                    Console.WriteLine($"Gender : {face.FaceAttributes.Gender}");
                    Console.WriteLine($"Glasses : {face.FaceAttributes.Glasses}");

                    // Get hair color
                    Hair hair = face.FaceAttributes.Hair;
                    string color = null;
                    if (hair.HairColor.Count == 0) { if (hair.Invisible) { color = "Invisible"; } else { color = "Bald"; } }
                    HairColorType returnColor = HairColorType.Unknown;
                    double maxConfidence = 0.0f;
                    foreach (HairColor hairColor in hair.HairColor)
                    {
                        if (hairColor.Confidence <= maxConfidence) { continue; }
                        maxConfidence = hairColor.Confidence; returnColor = hairColor.Color; color = returnColor.ToString();
                    }
                    Console.WriteLine($"Hair : {color}");

                    // Get more attributes
                    Console.WriteLine($"HeadPose : {string.Format("Pitch: {0}, Roll: {1}, Yaw: {2}", Math.Round(face.FaceAttributes.HeadPose.Pitch, 2), Math.Round(face.FaceAttributes.HeadPose.Roll, 2), Math.Round(face.FaceAttributes.HeadPose.Yaw, 2))}");
                    Console.WriteLine($"Makeup : {string.Format("{0}", (face.FaceAttributes.Makeup.EyeMakeup || face.FaceAttributes.Makeup.LipMakeup) ? "Yes" : "No")}");
                    Console.WriteLine($"Noise : {face.FaceAttributes.Noise.NoiseLevel}");
                    Console.WriteLine($"Occlusion : {string.Format("EyeOccluded: {0}", face.FaceAttributes.Occlusion.EyeOccluded ? "Yes" : "No")} " +
                        $" {string.Format("ForeheadOccluded: {0}", face.FaceAttributes.Occlusion.ForeheadOccluded ? "Yes" : "No")}   {string.Format("MouthOccluded: {0}", face.FaceAttributes.Occlusion.MouthOccluded ? "Yes" : "No")}");
                    Console.WriteLine($"Smile : {face.FaceAttributes.Smile}");
                    Console.WriteLine();
                
            
        }

        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }
    }
}
