using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class JsonParser
{
    public string age;
    public string gender;
    public string emotion;
    public string name;
    public Guid faceId;
    string str;
    public string personId;

    public JsonParser(string input) {
        str = input;
        name = "Someone";//getSpecific("faceId");
        personId = "";
        age = getSpecific("age");
        gender = getSpecific("gender").Contains("fe") ? "Female" : "Male";
        emotion = getMostLiklyEmotion();
        bool test = Guid.TryParse(getSpecific("faceId").TrimStart('"').TrimEnd('"'), out faceId);
        if (!test)
            faceId = new Guid();
    }

    public async Task<string> setName(string input) {
        
        if (input.Contains("personId")) {
            string output = getSpecific("personId", input).TrimStart('"').TrimEnd('"');
            //personId
            personId = output;
            FaceRequests req = new FaceRequests();
            output = await req.getName(output);
            //output = getSpecific("name", output);
            output = getSpecific("name", output).TrimStart('"').TrimEnd('"');

            

            return output;
        }
        
        return name;
    }

    private string getMostLiklyEmotion()
    {
        string output = "";
        double temp;
        double max = 0f;
        int maxI = 0;
        
        string[] emotionArr = { "Anger", "Contempt", "Disgust", "Fear", "Happiness", "Neutral", "Sadness", "Surprise"};


        for (int i = 0; i < emotionArr.Length; i++) {
            string info = getSpecific(emotionArr[i].ToLower());
            if (info == null) {
                return "Unknown";
            }
            if (Double.TryParse(info, out temp)){
                if (max < temp) {
                    max = temp;
                    maxI = i;
                }
            }
            
            //output += getSpecific(emotionArr[i].ToLower()) + " ";
        }
        output = emotionArr[maxI];
        return output;
    }

    string getSpecific(string category) {
        string output = "";

        int a = str.IndexOf(category);
        if (a == -1)
        {
            return null;
        }
        int b = str.IndexOf(':', a);
        if (b == -1)
        {
            return null;
        }
        int c = str.IndexOf(',', b);
        if (c == -1) {
            c = str.IndexOf('}', b);
        }
        output = str.Substring(b+1, c-(b+1));
        return output;
    }

    string getSpecific(string category, string input)
    {
        string output = "";

        int a = input.IndexOf(category);
        if (a == -1)
        {
            return null;
        }
        int b = input.IndexOf(':', a);
        if (b == -1)
        {
            return null;
        }
        int c = input.IndexOf(',', b);
        if (c == -1)
        {
            c = input.IndexOf('}', b);
        }
        output = input.Substring(b + 1, c - (b + 1));
        return output;
    }
}
