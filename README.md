# BaiduVoiceTest
Unity3d使用百度的Rest Api实现语音交互，可以将语音转化为文本，也可以将文本转化为语音。  
这个项目原本是为一个HoloLens眼镜的虚拟角色语音交互做准备。该平台上由于编译环境的问题接入SDK会出现种种问题，所以只能使用百度语音的RestApi来实现。 使用RestApi就可以不受平台的限制了。
## 语音解析
百度语音的RestApi接口实现语音转化为文本，只需要通过http请求发送本地录音所生成的音频文件，只不过请求需要的是pcm格式，采样率为1600的文件，录音完毕以后需要进行相应转码。
## 语音合成
在使用语音合成接口实现将文本转化为合成语音时，由于百度语音合成接口返回的音频文件是mp3格式的，出现了比较棘手的问题就是**Unity3D不支持网络获取到的mp3格式的音频文件**，这里还需要使用到一个NAudio插件，来实现将mp3音频转换成为unity3d可以直接播放的AudioClip。  
___  
整个项目文件结构如下：  
![image](https://note.youdao.com/yws/api/personal/file/FA630B4C09F14CEC96C10EA25E877615?method=download&shareKey=eaeb59ac7398da342890e5e3fb9ecbb9)  
把代码重构了一下，将整个工具类做成了单例，在使用时需要将需要将BaiduApiToolkit脚本绑定到有AudioSource组件的物体上才能播放语音。然后Wav脚本与Plugins中的NAudio都是音频转换过程中需要的。  
下面是调用UIManger中调用Baidu接口的代码：
```C#
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
```
之前本想在嵌入一个只能回复的接口，不过目前还没有实现，只是在BaiduApiToolkit脚本中写死了一个简单的回答。
## 关于接口的调用
在使用时只需要通过StratRecord()开始录音，然后再通过StopRecord()结束录音，不过StopRecord()方法会有一个异步回调函数，用于将解析完成后的文本返回到回调函数中。  
此外就是ReadText(string text)方法，这里的text参数就是要合成语音文本，传入文本之后，接口会自动播放语音。  
 
## 还有点小问题
在语音解析过程中，由于录音最大时长是写死的，并且无论说了多长时间，哪怕说了一秒钟，最后也按照最大时长生成音频并传输，所以解析效率有点低，原本想把音频文件压缩一下再发送，不过有点犯懒就不想写了 = =。不过百度语音的语音合成速度还是相当迅速的！还是相当令人满意的。




