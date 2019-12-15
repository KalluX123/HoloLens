using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.XR.WSA.WebCam;
using System;
using UnityEngine.XR.WSA;
using UnityEngine.UI;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.XR.WSA.Input;
using System.Threading;


#if WINDOWS_UWP
using HoloLensForCV;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.System.Threading;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

public class WebCamTest : MonoBehaviour
{
    string URL = "https://hololens-f3898.firebaseio.com/";
    string[] jsondb;
    string testtest = "";
    ArrayList myAL = new ArrayList();
    FaceRequests faceReq;
    string faceInfo = "a";
    bool infoReady = false;
    bool dBread = false;
    bool camOn = false;
    int timer = 0;
    GameObject[] arr;
    GameObject ob;
    bool alreadySaving = false;
    float lastTime = -10;
    bool newFace = true;

    Matrix4x4 webcamToWorldMatrix;
    Matrix4x4 projectionMatrix;
    bool imageInitialized;
    int width = 0;
    int height = 0;
    Vector3 camPos = new Vector3(0,0,0);
    Vector3 WorldSpaceRayPoint2 = new Vector3(0, 0, 0);

    int nrOfFaces = 0;
    bool alreadyUp = false;
    uint memeX = 0, memeY = 0, x = 0, y = 0;
    Ray r;
    GameObject clipBoard;
    bool doDrag = false;
    Vector3 lastHandPos = new Vector3(0,0,0);
    float distanceToObject = 0;
    Vector3 directionToObject = new Vector3(0, 0, 0);
    string path = "";
    JsonParser info;
#if WINDOWS_UWP && !UNITY_EDITOR
    MediaFrameSourceGroup source = new MediaFrameSourceGroup(MediaFrameSourceGroupType.PhotoVideoCamera, new SpatialPerception(), null);
    private FaceDetector faceDetector;
    IList<DetectedFace> faces;
    SoftwareBitmap sb;
#endif
    async void Start()
    {
        initDataBase();


        faceReq = new FaceRequests();
        
        clipBoard = GameObject.Find("ClipBoard");
        clipBoard.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);
        GameObject.Find("Exit").GetComponent<Renderer>().material.color = new Color(1,0,0);
        GameObject.Find("Exit").SetActive(false);
        InteractionManager.InteractionSourcePressed += StartDrag;
        InteractionManager.InteractionSourceUpdated += Drag;
        InteractionManager.InteractionSourceReleased += StopDrag;
        InteractionManager.InteractionSourceLost += LostDrag;
        arr = new GameObject[6];
        arr[0] = GameObject.Find("NamePlaceHolder");
        arr[1] = GameObject.Find("AgePlaceHolder");
        arr[2] = GameObject.Find("GenderPlaceHolder");
        arr[3] = GameObject.Find("EmotionPlaceHolder");
        arr[4] = GameObject.Find("WeightPlaceHolder");
        arr[5] = GameObject.Find("LengthPlaceHolder");
        ob = GameObject.Find("Cube");
       // var cubeRend = ob.GetComponent<Renderer>();
       // cubeRend.material.SetColor(
        clipBoard.SetActive(false);
        

#if WINDOWS_UWP && !UNITY_EDITOR
        faceDetector = await FaceDetector.CreateAsync();
        InitializeMediaCapture();
#endif
    }

    private void initDataBase()
    {
        StartCoroutine(ReqDB());
    }

    IEnumerator ReqDB()
    {
        using (WWW www = new WWW(URL+ ".json"))
        {

            //Thread.Sleep(2000);
            //Task.Delay(2000);

            //  yield return new WaitForSeconds(5);
            yield return new WaitForSeconds(5);
            string s = www.text;

            if (s == null)
            {
                StartCoroutine(ReqDB());
            }

            s = s.Replace("},", ":");
            s = s.Replace('"', ' ');
            s = s.Replace("{", "");
            s = s.Replace("}", "");
            s = s.Replace(",", " ");
            s = s.Replace("userAge", " ");
            s = s.Replace("userHeight", " ");
            s = s.Replace("userWeight", " ");
            s = s.Replace("userfName", " ");
            s = s.Replace("userlName", " ");
            s = s.Replace(" ", "");
            s = s.Replace("::", ":");

            string[] ss = s.Split(':');
            testtest = ss[1];

            for (int i = 0; i < ss.Length; i += 6)
            {
                myAL.Add(new User(ss[i], ss[i + 1], ss[i + 2], ss[i + 3], ss[i + 4], ss[i + 5]));
            }



           // yield return www;

            // Debug.Log(ss[5]);
        }
    }

    private void StartDrag(InteractionSourcePressedEventArgs args)
    {
        Vector3 pos;
        if (args.state.sourcePose.TryGetPosition(out pos))
        {
            Ray check = new Ray(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward));
            RaycastHit hit;
            if (Physics.Raycast(check, out hit)) {

                if (hit.collider.name.Equals("ClipBoard"))
                {
                    doDrag = true;
                    distanceToObject = hit.distance;
                    directionToObject = clipBoard.transform.position - pos;
                }
                else if (hit.collider.name.Equals("Exit"))
                {
                    clipBoard.SetActive(false);
                }
                else {
                    clipBoard.transform.position = pos + Camera.main.transform.TransformDirection(Vector3.forward) * 0.2f;
                }
            }
            else
            {
                clipBoard.SetActive(false);
            }
        }
    }

    private void StopDrag(InteractionSourceReleasedEventArgs args)
    {
        doDrag = false;
    }

    private void LostDrag(InteractionSourceLostEventArgs args)
    {
        doDrag = false;
    }

    private void Drag(InteractionSourceUpdatedEventArgs args)
    {
        Vector3 pos;

        if (doDrag) {
            if (args.state.sourcePose.TryGetPosition(out pos)) {
                clipBoard.transform.position = pos + directionToObject;
            }
        }

    }

    async void OnApplicationFocus(bool hasFocus) {
        if (hasFocus && !camOn) {
#if WINDOWS_UWP && !UNITY_EDITOR
        faceDetector = await FaceDetector.CreateAsync();
        InitializeMediaCapture();
#endif
        }
        else if(!hasFocus){
            camOn = false;
            infoReady = false;
            clipBoard.SetActive(false);
            alreadyUp = false;
            alreadySaving = false;
        }
    }

    async void Update()
    {
        if (camOn) {
#if WINDOWS_UWP && !UNITY_EDITOR
        int lastNrOfFaces = nrOfFaces;
        if (timer >= 40) {
        
            bool success = await detectFaces();
            timer = 0;
            if(success){
                nrOfFaces = faces.Count;
                
                if((!clipBoard.activeInHierarchy || (Time.time > lastTime)) && nrOfFaces > 0){
                    saveSnapShot(sb);
                }
            }
            else{
                nrOfFaces = 0;
            }
            if(nrOfFaces == 0)
                newFace = true;
        }
#endif
            alreadyUp = placeHolograms(alreadyUp);
            timer++;
            clipBoard.transform.LookAt(Camera.main.transform);
            clipBoard.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        }
    }

#if WINDOWS_UWP && !UNITY_EDITOR
    private async void ShowPicture()
{
    var data = await EncodedBytes(sb, BitmapEncoder.JpegEncoderId);
    Texture2D pic = new Texture2D(sb.PixelWidth, sb.PixelHeight);
    pic.LoadImage(data);
    GameObject.Find("Quad").GetComponent<Renderer>().material.mainTexture = pic;
}

private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
{
    byte[] array = null;
    
    using (var ms = new InMemoryRandomAccessStream())
    {
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
        encoder.SetSoftwareBitmap(soft);

        try
        {
            await encoder.FlushAsync();
        }
        catch ( Exception ex ){ return new byte[0]; }

        array = new byte[ms.Size];
        await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
    }
    return array;
}
#endif
    private Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
    {
        Vector3 from = new Vector3(0, 0, 0);
        if (proj == null || to == null)
            return from;
        var axsX = proj.GetRow(0);
        var axsY = proj.GetRow(1);
        var axsZ = proj.GetRow(2);
        from.z = to.z / axsZ.z;
        from.y = (to.y - (from.z * axsY.z)) / axsY.y;
        from.x = (to.x - (from.z * axsX.z)) / axsX.x;
        return from;
    }

    public void changeBoard(User user, JsonParser info2)
    {
                if (user.userlName.Equals("UNK") || (user.userfName.Equals("UNK")))
                {
                    arr[0].GetComponent<TextMeshPro>().SetText(info2.name);
                }
                else
                {
                    arr[0].GetComponent<TextMeshPro>().SetText(user.userfName + " " + user.userlName);
                }

                if (user.userAge.Equals("UNK"))
                {
                    arr[1].GetComponent<TextMeshPro>().SetText(info2.age);
                }
                else
                {
                    arr[1].GetComponent<TextMeshPro>().SetText(user.userAge);
                }

                arr[2].GetComponent<TextMeshPro>().SetText(info2.gender);
                arr[3].GetComponent<TextMeshPro>().SetText(info2.emotion);

                if (user.userHeight.Equals("UNK"))
                {
                    arr[5].GetComponent<TextMeshPro>().SetText("---");
                }
                else
                {

                    arr[5].GetComponent<TextMeshPro>().SetText(user.userHeight+ " cm");

                }

                if (user.userWeight.Equals("UNK"))
                {
                    arr[4].GetComponent<TextMeshPro>().SetText("---");
                }
                else
                {
                    arr[4].GetComponent<TextMeshPro>().SetText(user.userWeight +" kg");
                }      
        
    }

    private string[] parseDb(string s)
    {
        s = s.Replace('"', ' ');
        s = s.Replace("{", "");
        s = s.Replace("}", "");
        s = s.Replace(",", " ");
        s = s.Replace("userAge", " ");
        s = s.Replace("userHeight", " ");
        s = s.Replace("userWeight", " ");
        s = s.Replace("userfName", " ");
        s = s.Replace("userlName", " ");
        s = s.Replace(" ", "");
        s = s.Substring(1);

        string[] ss = s.Split(':');
        if (ss[0].Equals("ull"))
        {
            ss = null;
        }

        return ss;
    }

    bool placeHolograms(bool placedAlready)
    {
        if (nrOfFaces > 0 && infoReady)
        {
            
                if (!placedAlready)
                {
                    placedAlready = true;

                bool temp = false;
                foreach (User u in myAL)
                {
                    if (info.personId.Equals(u.userID))
                    {
                        changeBoard(u, info);
                        temp = true;
                    }
                }

                if (temp == false)
                {
                    arr[0].GetComponent<TextMeshPro>().SetText(info.name);
                    arr[1].GetComponent<TextMeshPro>().SetText(info.age);
                    arr[2].GetComponent<TextMeshPro>().SetText(info.gender);
                    arr[3].GetComponent<TextMeshPro>().SetText(info.emotion);
                    arr[4].GetComponent<TextMeshPro>().SetText("---");
                    arr[5].GetComponent<TextMeshPro>().SetText("---");
                }



                clipBoard.SetActive(true);

                    RaycastHit hit;
                    if (Physics.Raycast(Camera.main.transform.position, WorldSpaceRayPoint2, out hit) && newFace)
                    {
                        clipBoard.transform.position = hit.point;
                        lastTime = Time.time + 5f;
                        newFace = false;
                    }


#if WINDOWS_UWP && !UNITY_EDITOR
                    //ShowPicture();
#endif

            }

            
        }
        else
        {
            /*
            for (int i = 0; i < jsondb.Length; i++)
            {
                jsondb[i] = "";
            }
            */


            placedAlready = false;
            infoReady = false;
            clipBoard.SetActive(false);
        }
        return placedAlready;
    }



#if WINDOWS_UWP && !UNITY_EDITOR
    async Task<bool> saveSnapShot(SoftwareBitmap input) {
        if(input == null || alreadySaving){
            return false;
        }
        alreadySaving = true;
        try{
            SoftwareBitmap copy = SoftwareBitmap.Copy(input);
            
            StorageFolder localFolder = KnownFolders.PicturesLibrary;
            StorageFile sampleFile = await localFolder.CreateFileAsync("dataFile.jpeg", CreationCollisionOption.ReplaceExisting);
            // Create an encoder with the desired format
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, await sampleFile.OpenAsync(FileAccessMode.ReadWrite));
        
            // Set the software bitmap
            encoder.SetSoftwareBitmap(copy);
            //Save Image
            await encoder.FlushAsync();
            path = sampleFile.Path;
            faceInfo = await faceReq.MakeAnalysisRequest(path);
            if (!faceInfo.Contains("error") && faceInfo.Contains("gender"))//Primitive checks, but they work
            {
                info = new JsonParser(faceInfo);
                string temp = await faceReq.recognizeImage(info.faceId);
                info.name = await info.setName(temp);
                infoReady = true;
                alreadyUp = false;
            }
        }
        catch(Exception e){
            alreadySaving = false;
            return false;
        }
        alreadySaving = false;
        return true;
    }
#endif

#if WINDOWS_UWP && !UNITY_EDITOR
    async Task<bool> detectFaces()
    {
    bool success = true;
    try{
        sb = GetImage();
        faces = null;
        if(sb == null){
            return false;
        }
        SoftwareBitmap converted = SoftwareBitmap.Convert(sb, BitmapPixelFormat.Nv12);

        faces = await this.faceDetector.DetectFacesAsync(converted);

        var ite = faces.GetEnumerator();
        ite.MoveNext();
        memeX = ite.Current.FaceBox.Width;
        memeY = ite.Current.FaceBox.Height;
        x = ite.Current.FaceBox.X;
        y = ite.Current.FaceBox.Y;

        Vector2 ImagePosZeroToOne = new Vector2(1.0F * (x+(memeX/2)) / (1.0F * width), 1.0F - 1.0F * (y + (memeY / 2)) / (1.0F * height));
        Vector2 ImagePosProjected = ((ImagePosZeroToOne * 2.0F) - new Vector2(1.0F, 1.0F)); // -1 to 1 space
        Vector3 CameraSpacePos = UnProjectVector(projectionMatrix, new Vector3(ImagePosProjected.x, ImagePosProjected.y, 15));
        WorldSpaceRayPoint2 = webcamToWorldMatrix * CameraSpacePos; // ray point in world space
    }
    catch(Exception e){
        success = false;
    }
    return success;
    }
#endif


#if WINDOWS_UWP && !UNITY_EDITOR
    async Task InitializeMediaCapture()
    {
    if(!camOn)
        await source.StartAsync();

        camOn = true;
    }
#endif

#if WINDOWS_UWP && !UNITY_EDITOR
    public SoftwareBitmap GetImage()
    {
        if (!camOn)
            return null;
        
        SensorFrame latestFrame;
        latestFrame = source.GetLatestSensorFrame(SensorType.PhotoVideo);

        if (latestFrame == null)
            return null;

        webcamToWorldMatrix.m00 = latestFrame.FrameToOrigin.M11;
        webcamToWorldMatrix.m01 = latestFrame.FrameToOrigin.M21;
        webcamToWorldMatrix.m02 = latestFrame.FrameToOrigin.M31;

        webcamToWorldMatrix.m10 = latestFrame.FrameToOrigin.M12;
        webcamToWorldMatrix.m11 = latestFrame.FrameToOrigin.M22;
        webcamToWorldMatrix.m12 = latestFrame.FrameToOrigin.M32;

        webcamToWorldMatrix.m20 = -latestFrame.FrameToOrigin.M13;
        webcamToWorldMatrix.m21 = -latestFrame.FrameToOrigin.M23;
        webcamToWorldMatrix.m22 = -latestFrame.FrameToOrigin.M33;

        webcamToWorldMatrix.m03 = latestFrame.FrameToOrigin.Translation.X;
        webcamToWorldMatrix.m13 = latestFrame.FrameToOrigin.Translation.Y;
        webcamToWorldMatrix.m23 = -latestFrame.FrameToOrigin.Translation.Z;
        webcamToWorldMatrix.m33 = 1;


        if (imageInitialized == false)
        {
            height = latestFrame.SoftwareBitmap.PixelHeight;
            width = latestFrame.SoftwareBitmap.PixelWidth;

            projectionMatrix = new Matrix4x4();
            projectionMatrix.m00 = 2 * latestFrame.CameraIntrinsics.FocalLength.X / width;
            projectionMatrix.m11 = 2 * latestFrame.CameraIntrinsics.FocalLength.Y / height;
            projectionMatrix.m02 = -2 * (latestFrame.CameraIntrinsics.PrincipalPoint.X - width / 2) / width;
            projectionMatrix.m12 = 2 * (latestFrame.CameraIntrinsics.PrincipalPoint.Y - height / 2) / height;
            projectionMatrix.m22 = -1;
            projectionMatrix.m33 = -1;

    
            imageInitialized = true;
        }

        camPos = Camera.main.transform.position;
        return latestFrame.SoftwareBitmap;
    }
#endif

}