using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using UnityEngine;

class TokenResponse
{
    public string access_token;
}

//语音转换文本返回结果json样式：text:{"corpus_no":"6495663114784586723","err_msg":"success.","err_no":0,"result":["87076677，"],"sn":"16674858431512389423"}
class STTResult
{
    public string[] result;
    public string err_msg;
    public string corpus_no;
}
//文本装换成音频，如果合成错误返回json样式：{"err_no":500,"err_msg":"notsupport.","sn":"abcdefgh","idx":1} 
class TTSRestul
{
    public int   err_no;
    public string err_msg;
}


public class BaiduApiToolkit:MonoBehaviour
{
    private static BaiduApiToolkit _instance;

    public static BaiduApiToolkit Instance
    {
        get { return _instance; }
    }
    //通过常量设置各种Key
    private const string SecretKey = "cTNTKa95UCD12BtXnDRodrTfEqLT4lFw";
    private const string APIKey = "BcYilT8n4GHwanvA055MN2sB";
    private string Token = null;

    //语音合成所需参数
    private const string lan = "zh";//语言
    private const string per = "4";//发音人选择 0位女  1位男  默认 女0为女声，1为男声，3为情感合成-度逍遥，4为情感合成-度丫丫，默认为普通女声
    private const string ctp = "1";//客户端类型选择 web端为1  
    private const string spd = "3";//范围0~9  默认 5   语速
    private const string pit = "4";//范围0~9  默认 5   音调
    private const string vol = "5";//范围0~9  默认 5   音量
    //录音所需属性
    private int maxRecorTime=10;//录音时间
    private AudioClip clipRecord;
    private bool isRecording=false;
    private static string deviceName = "baiduRecorder";  //microphone设备名称
    private AudioSource audioSource;
    public delegate void RecognizeComplete(string result);//回调函数定义
    void Start()
    {
        _instance = this;
        audioSource = this.GetComponent<AudioSource>();
        StartCoroutine(GetAccessToken());
    }
    /// <summary>
    /// 获取Token
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetAccessToken()
    {
        var uri =
            "https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials&client_id="+APIKey+"&client_secret="+SecretKey;
        var www = new WWW(uri);
        yield return www;
        if (string.IsNullOrEmpty(www.error))
        {
            var result = JsonUtility.FromJson<TokenResponse>(www.text);
            Token = result.access_token;
        }
        else
        {
            Debug.LogError(www.error);
        }
    }
    public bool IsRecording()
    {
        return isRecording;
    }
    /// <summary>
    /// 开始录音
    /// </summary>
    public void StartRecord()
    {
        isRecording = true;
        audioSource.Stop();      
        if (Microphone.IsRecording(deviceName))
        {
            return;
        }
        clipRecord = Microphone.Start(deviceName, false, maxRecorTime, 16000);
    }
   
    /// <summary>
    /// 停止录音,将语音保存成文件
    /// </summary>
    public void StopRecord(RecognizeComplete callback)
    {
        isRecording = false;
        if (Microphone.IsRecording(deviceName))
        {
            Microphone.End(deviceName);
        }
       StartCoroutine(Recognize(ClipToByteArray(clipRecord), delegate(string result)
       {
           callback(result);
       }));      
    }
    /// <summary>
    /// 将录音转换为PCM16格式数据
    /// </summary>
    /// <param name="clip">录音音频</param>
    /// <returns></returns>
    public static byte[] ClipToByteArray(AudioClip clip)
    {      
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        var samples_int16 = new short[samples.Length];

        for (var index = 0; index < samples.Length; index++)
        {
            var f = samples[index];
            samples_int16[index] = (short)(f * short.MaxValue);
        }
        var byteArray = new byte[samples_int16.Length * 2];
        Buffer.BlockCopy(samples_int16, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }
  
    /// <summary>
    /// 将转换后符合格式要求的音频数据以字节形式发送百度语音服务器
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public IEnumerator Recognize(byte[] data,RecognizeComplete callback)
    {
        if (!string.IsNullOrEmpty(Token))
        {
            var uri =
                "http://vop.baidu.com/server_api?lan=zh&cuid={SystemInfo.deviceUniqueIdentifier}&token=" + Token;
            var headers = new Dictionary<string, string> { { "Content-Type", "audio/pcm;rate=16000" } };
            var www = new WWW(uri, data, headers);
            yield return www;
            Debug.Log(www.text);
            if (string.IsNullOrEmpty(www.error))
            {
                Debug.Log("text:" + www.text);
                var wwwResult = JsonUtility.FromJson<STTResult>(www.text);
                string resultContent;
                if (wwwResult.err_msg != "success.")
                {
                    resultContent = null;
                }
                else
                {
                    resultContent = "您说的是：" + wwwResult.result[0];
                }
                callback(resultContent);
                Debug.Log("result:" + resultContent);                 
            }
            else
            {
                Debug.LogError("error:" + www.error);
            }
        }       
    }
   
    /// <summary>
    /// 回答用户提问
    /// </summary>
    /// <param name="question">用户语音问题的字符串</param>
    public string AnswerQuestion(string question)
    {
        string answer ="很高兴为您服务！";
        if (question==null)
        {
            answer = "抱歉，不能识别语音，请重复一遍！";           
        }
        else if (question.Contains("你好") || question.Contains("您好"))
        {
            answer = "您好，请问有什么需要帮助的？";
        }
        return answer;
    }

    private  IEnumerator TextToSpeech(string text)
    {
        string cuid = SystemInfo.deviceUniqueIdentifier;
        StartCoroutine(GetAccessToken());
        Debug.Log("token:"+this.Token);

        string uri = "http://tsn.baidu.com/text2audio?" + "tex=" + text + "&lan=" + lan + "&per=" + per +
            "&ctp=" + ctp + "&cuid="+cuid+"&tok=" + Token + "&spd=" + spd + "&pit=" + pit + "&vol=" + vol;
        Debug.Log("uri"+uri);
        WWW www=new WWW(uri);
        yield return www;
        audioSource = this.GetComponent<AudioSource>();
        audioSource.clip = FromMp3Data(www.bytes);
        Debug.Log(audioSource==null);
        audioSource.Play();
    }

    public void ReadText(string text)
    {
        StartCoroutine(TextToSpeech(text));
    }
    /// <summary>
    /// 将mp3格式的字节数组转换为audioclip
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static AudioClip FromMp3Data(byte[] data)
    {
        // Load the data into a stream  
        MemoryStream mp3stream = new MemoryStream(data);
        // Convert the data in the stream to WAV format  
        Mp3FileReader mp3audio = new Mp3FileReader(mp3stream);

        WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(mp3audio);
        // Convert to WAV data  
        Wav wav = new Wav(AudioMemStream(waveStream).ToArray());
        //Debug.Log(wav);  
        AudioClip audioClip = AudioClip.Create("testSound", wav.SampleCount, 1, wav.Frequency, false);
        audioClip.SetData(wav.LeftChannel, 0);
        // Return the clip  
        return audioClip;
    }
    private static MemoryStream AudioMemStream(WaveStream waveStream)
    {
        MemoryStream outputStream = new MemoryStream();
        using (WaveFileWriter waveFileWriter = new WaveFileWriter(outputStream, waveStream.WaveFormat))
        {
            byte[] bytes = new byte[waveStream.Length];
            waveStream.Position = 0;
            waveStream.Read(bytes, 0, Convert.ToInt32(waveStream.Length));
            waveFileWriter.Write(bytes, 0, bytes.Length);
            waveFileWriter.Flush();
        }
        return outputStream;
    }
}


