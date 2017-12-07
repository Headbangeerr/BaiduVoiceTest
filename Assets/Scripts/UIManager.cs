using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

    private AudioClip _clipRecord = new AudioClip();
    private Text titleText;
    private Text resultText;
    private Text answerText;

    private static UIManager _instance;

    public static UIManager Instance
    {
        get { return _instance; }
    }
    private void Start()
    {
        _instance = this;
        titleText = GameObject.Find("Title").GetComponent<Text>();
        resultText = GameObject.Find("ResultText").GetComponent<Text>();
        answerText = GameObject.Find("AnswerText").GetComponent<Text>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
           BaiduApiToolkit.Instance.StartRecord();          
            titleText.text = "正在聆听……";
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            titleText.text = "请摁住”S“键开始说话";
            resultText.text = "正在识别……";
            BaiduApiToolkit.Instance.StopRecord(delegate(string result)//通过回调获取协程中通过网络传回的语音文本
            {
                if (result==null)//如果返回null代表解析失败
                {
                    this.resultText.text = "抱歉，不能识别语音，请重复一遍！";
                }
                else
                {
                    this.resultText.text = result;
                }
                string answer = BaiduApiToolkit.Instance.AnswerQuestion(result);
                answerText.text = "回答：" + answer;
                BaiduApiToolkit.Instance.ReadText(answer);
            });
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            BaiduApiToolkit.Instance.ReadText("你好呀");
        }
    }
}
