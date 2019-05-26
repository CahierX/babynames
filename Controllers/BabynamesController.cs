using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}
namespace BabyNames.Controllers
{
    public class BabynamesController : ApiController
    {
        public dynamic getYear(int year)
        {
            try
            {
                string txtFile = "C:/Users/Administrator/Desktop/babynames/names/yob" + year + ".txt";
                StreamReader srReadFile = new StreamReader(txtFile);
                return srReadFile;
            }
            catch
            {
                return null;
            }
        }

        public StreamReader fs(string filName)
        {
            try
            {
                string path = System.Web.Hosting.HostingEnvironment.MapPath(@"/cmu/" + filName);

                StreamReader srReadFile = new StreamReader(path);
                return srReadFile;
            }
            catch
            {
                return null;
            }
        }
        public List<dynamic> loadData(int start, int end)
        {
            List<dynamic> data = new List<dynamic>(); // 储存txt 转换到 json的数据
            for (int i = start; i <= end; i++) // 此处的i也是当前操作的文件的年份
            {

                JObject totals;

                // 读totals文件
                string totalsJsonFile = "C:/Users/Administrator/Desktop/babynames/babynames-master/babynames-master/extra/totals.json";
                System.IO.StreamReader totalsJsonReader = System.IO.File.OpenText(totalsJsonFile);
                using (JsonTextReader reader = new JsonTextReader(totalsJsonReader))
                {
                    totals = (JObject)JToken.ReadFrom(reader); // 将totals文件转换成JObject类型
                }

                StreamReader srReadFile = getYear(i); // 获取year的txt文件数据;
                if (srReadFile == null)
                {
                    continue;
                }
                // 读取流直至文件末尾结束
                while (!srReadFile.EndOfStream)
                {
                    Dictionary<string, dynamic> dataTemp = new Dictionary<string, dynamic>(); // 储存一个文件的数据
                    string strReadLine = srReadFile.ReadLine(); // 读取每行数据
                    string[] strReadLineList = strReadLine.Split(new Char[] { ',' }); // 将数据按逗号分割放入数组
                    if (strReadLineList == null) // 为空continue
                    {
                        continue;
                    }
                    Dictionary<string, dynamic> names = new Dictionary<string, dynamic>();
                    names.Add("name", strReadLineList[0]);
                    names.Add("gender", strReadLineList[1]);
                    names.Add("value", Convert.ToInt32(strReadLineList[2]));

                    decimal percent = names["value"] / Convert.ToDecimal(totals["" + i + ""][names["gender"]]); // 计算公式
                    decimal percents = percent;
                    decimal.TryParse(percent.ToString("f22"), out percents); // 保留22位小数,并返回decimal类型
                    dataTemp.Add("year", i);
                    dataTemp.Add("totals", totals);
                    dataTemp.Add("names", names);
                    dataTemp.Add("percent", percents);
                    data.Add(dataTemp);
                }
            }
            return data;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">只截取name的数据</param>
        /// <param name="start">开始年份</param>
        /// <param name="end">结束年份</param>
        /// <param name="cutoff"></param>
        /// <param name="min"></param>
        [HttpGet]
        public void Index(Boolean store = true, Boolean phonemes = true, string name = "", int start = 1880, int end = 2017, double cutoff = 0, double min = 0)
        {

            JObject result = new JObject();
            List<string> dataId = new List<string>();

            if (store) // 使用roster文件读取json
            {
                JArray totals;
                List<dynamic> data = new List<dynamic>(); // 储存json文件里的数据
                string totalsJsonFile = "C:/Users/Administrator/Desktop/babynames/flat/roster/roster.json";
                System.IO.StreamReader totalsJsonReader = System.IO.File.OpenText(totalsJsonFile);
                using (JsonTextReader reader = new JsonTextReader(totalsJsonReader))
                {
                    totals = (JArray)JToken.ReadFrom(reader); // 将totals文件转换成JObject类型
                }
                foreach (string i in totals)
                {
                    dataId.Add(i);
                    // i 为文件的name 根据i去读取文件
                    JObject dataSingleJson; // 保存每个小文件的json
                    string cmud = CmuDictAsync(i); // 获取每个名称的音素
                    string jsonFile = "C:/Users/Administrator/Desktop/babynames/flat/" + i + ".json";
                    System.IO.StreamReader jsonFileReader = System.IO.File.OpenText(jsonFile);
                    using (JsonTextReader singleReader = new JsonTextReader(jsonFileReader))
                    {
                        dataSingleJson = (JObject)JToken.ReadFrom(singleReader); // 将totals文件转换成JObject类型
                        result.Add(i, dataSingleJson);
                    }
                }
            }
            else
            {
                List<dynamic> filesTxt = new List<dynamic>();

                filesTxt = loadData(start, end); // 使用txt读取转成json
                if (name != "")
                {
                    filesTxt = filesTxt.Where(p => p["names"]["name"] == name).ToList(); // 只截取name的数据
                }
                JObject data = new JObject();
                string id, KValues;
                int VValues;
                decimal PValues;
                JObject nullData = new JObject();
                JObject nodePercents, nodeValues;
                int filesCount = filesTxt.Count; // 单独写出来提升循环速度

                for (int i = 0; i < filesCount; i++) // 倒叙提升速度
                {
                    var filesList = filesTxt[i]["names"];
                    id = filesList["name"] + "-" + filesList["gender"];
                    if (!dataId.Contains(id)) // 不存在id的时候新增一条数据
                    {
                        KValues = Convert.ToString(filesTxt[i]["year"]);
                        VValues = filesTxt[i]["names"]["value"];
                        PValues = filesTxt[i]["percent"];
                        dataId.Add(id);
                        data.Add("_id", id);
                        data.Add("name", filesList["name"]);
                        data.Add("gender", filesList["gender"]);
                        data.Add("values", new JObject(new JProperty(KValues, new JValue(VValues))));
                        data.Add("percents", new JObject(new JProperty(KValues, new JValue(PValues))));
                        data.Add("normalized", nullData);
                        result.Add(id, data);
                        data = new JObject();
                        continue;
                    }
                    // 在把新的数据加进去
                    nodePercents = result[id].SelectToken("percents") as JObject;
                    nodePercents.Add(new JProperty(Convert.ToString(filesTxt[i]["year"]), filesTxt[i]["percent"])); // 在percents下面新增数据
                    nodeValues = result[id].SelectToken("values") as JObject;
                    nodeValues.Add(new JProperty(Convert.ToString(filesTxt[i]["year"]), filesTxt[i]["names"]["value"])); // 同理
                }
            }
            string path;
            if (!store)
            {
                path = "C:/Users/Administrator/Desktop/babynames/flatTemp"; // 读取txt转json保存的文件夹
            }
            else
            {
                path = "C:/Users/Administrator/Desktop/babynames/data"; //读取json 保存的文件夹

            }
            // 生成音律文件
            if (phonemes)
            {
                JObject phonemesResult = new JObject();
                JObject tempPhonemesResult = (JObject)result.DeepClone();// 将result 复制一份,因为接下来的Peaks和Pronunciation会改变result,DeepClone()为深拷贝,简单的赋值依旧会改变result

                Pronunciation(Peaks(tempPhonemesResult)); // 去计算获取pronunciation属性的值和peaks属性的值
                foreach (var i in tempPhonemesResult) // 遍历最后的结果,写入json文件中
                {
                    dynamic isHavePronunciation = i.Value["pronunciation"];
                    var phoneme = i.Value["pronunciation"].ToString().Split(new char[2] { ' ', ' ' });

                    if (isHavePronunciation == null) // 如果这个名字没有音素直接跳过
                    {
                        continue;
                    }
                    else
                    {
                        if (phonemesResult[phoneme[0]] == null) //如果不存在初始化文件格式
                        {
                            JObject tempData = new JObject();
                            tempData.Add("percents", new JObject());
                            tempData.Add("names", new JArray());
                            phonemesResult.Add(phoneme[0], tempData);
                        }

                    }
                    JObject tempNames = new JObject();
                    tempNames.Add("name", i.Value["name"]);
                    tempNames.Add("peak", MapValue(i.Value["peaks"]["percents"]["percents"]));
                    JArray phonemesNames = phonemesResult[phoneme[0]].SelectToken("names") as JArray;
                    phonemesNames.Add(tempNames);  // 往names下面填写name,peak属性值

                    for (var y = start; y <= end; y += 1) // 找出这个音素下所有名称对应的percents值,如果这个音素有这一年的值直接累加,若没有则新增
                    {
                        JObject JPercents = phonemesResult[phoneme[0]].SelectToken("percents") as JObject;
                        decimal tempDecimal = 0;

                        if (!JPercents.ContainsKey(y.ToString())) // 不存在就 新增
                        {
                            tempDecimal = 0;
                            tempDecimal += Convert.ToDecimal(i.Value["percents"].SelectToken(y.ToString()));
                            JPercents.Add(y.ToString(), tempDecimal);
                        }
                        else // 存在就累加
                        {
                            tempDecimal = Convert.ToDecimal(JPercents.GetValue(y.ToString()));
                            tempDecimal += Convert.ToDecimal(i.Value["percents"].SelectToken(y.ToString())); // 取出相同年份的值进行叠加
                            JPercents.SelectToken(y.ToString()).Replace(tempDecimal); // 替换指定年份的值
                        }
                    }

                }
                // 将操作完成后的数据生成json文件
                JArray data = new JArray();
                foreach (var d in phonemesResult)
                {
                    JObject temp = new JObject();

                    temp.Add("phoneme", d.Key);

                    JArray array = JArray.Parse(d.Value["names"].ToString());
                    JArray sorted = new JArray(array.OrderByDescending(obj => obj["peak"])); // 按照peak降序,将peak最大的名字放在第一位
                    JArray namesList = new JArray();
                    foreach (var p in sorted)
                    {
                        namesList.Add(p["name"].ToString()); // 依次添加进去
                    }
                    temp.Add("names", namesList);

                    JObject arrayPercents = JObject.Parse(d.Value["percents"].ToString());
                    JObject sortedPercents = new JObject(arrayPercents.Properties().Where(p => (decimal)p.Value != 0)); // 按照peak降序,将peak最大的名字放在第一位

                    JArray jArray = new JArray();
                    JObject tempPercents = new JObject();
                    foreach (var pe in sortedPercents)
                    {
                        tempPercents.Add("key", pe.Key);
                        tempPercents.Add("value", pe.Value);
                        jArray.Add(tempPercents);
                        tempPercents = new JObject();
                    }
                    temp.Add("percents", jArray);
                    data.Add(temp);
                    temp = new JObject();

                }
                string pathPhonemes = path + "/roster";
                string fileNamePhonemes = "/phonemes.json";// 生成phonemes文件
                FileStr(pathPhonemes, fileNamePhonemes, data, true);

            }

            // 获取至少出来了多少年的名字
            if (cutoff != 0)
            {
                int allYearCount = 0;
                JObject tempResult = new JObject(result); // 因为下面要修改result 所有这里将要遍历的数据放在一个临时文件中
                foreach (var i in tempResult)
                {
                    JToken json = i.Value;
                    foreach (JProperty JP in json)
                    {
                        if (JP.Name == "values") // 只查找key值为values
                        {
                            allYearCount = JP.Value.Count(); // 长度即为该名称出现的年份次数
                            if (allYearCount < cutoff) // 小于给定的cutoff移除
                            {
                                result.Remove(i.Key); // 移除不符合条件的数据
                            }
                        }
                    }

                }
            }
            // 获取在一年内至少出现多少次的名字
            if (min != 0)
            {
                JObject minResult = new JObject();
                foreach (var i in result)
                {
                    JToken json = i.Value;
                    foreach (JProperty JP in json)
                    {
                        if (JP.Name == "values") // 只查找key值为values
                        {
                            foreach (int m in JP.Value)
                            {
                                if (m >= min)
                                {
                                    minResult.Add(i.Key, i.Value); break; // 将符合条件的数据新增等minResult中
                                }
                            }
                        }
                    }

                }
                result = minResult; // 替换原来的result文件
            }
            foreach (var i in result) // 遍历最后的结果,写入json文件中
            {
                string singleFileName = "/" + i.Key + ".json";
                JToken json = i.Value;
                FileStr(path, singleFileName, json); // 生成json文件
            }

            // 生成phonemes文件代码


            // 生成roster的代码
            path = path + "/roster";
            string fileName = "/roster.json";// 生成roster文件
            FileStr(path, fileName, dataId);
            string fileName_short = "/roster_short.json"; // 生成roster_short文件,此文件包含姓名即有男又有女
            List<string> roster_short = new List<string>();
            foreach (string i in dataId)
            {
                string SingleName = i.Split('-')[0].ToString();
                string SingleGender = i.Split('-')[1].ToString();
                if (SingleGender == "F")
                {
                    if (dataId.IndexOf(SingleName + "-M") != -1)
                    {
                        roster_short.Add(SingleName + " (F)");
                    }
                    else
                    {
                        roster_short.Add(SingleName);
                    }
                }
                else
                {
                    if (dataId.IndexOf(SingleName + "-F") != -1)
                    {
                        roster_short.Add(SingleName + " (M)");
                    }
                    else
                    {
                        roster_short.Add(SingleName);
                    }
                }
            }
            FileStr(path, fileName_short, roster_short);
        }
        public dynamic Peaks(JObject data)
        {
            JObject result = new JObject();

            foreach (var d in data)
            {
                JObject peaks = d.Value as JObject;
                JObject peak = new JObject();
                JObject tempResult = entries(d.Value["values"]);
                int peakValue = 0;
                int yearKey = 0;
                foreach (var year in tempResult)
                {
                    // 找到最大值
                    int yearValue = Convert.ToInt32(year.Value);
                    if (peakValue < yearValue)
                    {
                        peakValue = yearValue;
                        yearKey = Convert.ToInt32(year.Key);
                    }

                }
                peak.Add(yearKey.ToString(), peakValue);
                result.Add("value", peak);
                peaks.Add(new JProperty("peaks", result));
                result = new JObject();
            }
            foreach (var d in data)
            {
                JObject peaks = d.Value["peaks"] as JObject;
                JObject peak = new JObject();
                JObject tempResult = entries(d.Value["percents"]);
                decimal peakValue = 0;
                int yearKey = 0;
                foreach (var year in tempResult)
                {
                    // 找到最大值
                    decimal yearValue = Convert.ToDecimal(year.Value);
                    if (peakValue < yearValue)
                    {
                        peakValue = yearValue;
                        yearKey = Convert.ToInt32(year.Key);
                    }

                }
                peak.Add(yearKey.ToString(), peakValue);
                result.Add("percents", peak);
                peaks.Add(new JProperty("percents", result));
                result = new JObject();

            }
            return data;
        }
        public JObject entries(dynamic map)
        {
            JObject entries = new JObject();
            foreach (var i in map) // 遍历最后的结果,写入json文件中
            {
                entries.Add(i.Name, i.Value);
            }
            return entries;

        }
        public dynamic MapValue(dynamic map)
        {
            dynamic a = 0;
            foreach (var i in map) // 遍历最后的结果,写入json文件中
            {
                a = i.Value;
            }
            return a;
        }
        public void FileStr(string path, string fileName, dynamic data, Boolean removeSpace = false)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            using (FileStream fs = new FileStream(path + fileName, FileMode.Create))
            {
                //写入
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    if (data is JToken) // 如果是string类型则data不需要序列化
                    {
                        if (removeSpace) // 是否去除空格, 生成phonemes文件需要去除空格回车等
                        {
                            sw.WriteLine(data.ToString().Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", ""));
                        }
                        else
                        {
                            sw.WriteLine(data);
                        }
                    }
                    else
                    {
                        sw.Write(JsonConvert.SerializeObject(data));
                    }
                }

            }
        }
        CMUDict cache = new CMUDict();

        public void Pronunciation(JObject data)
        {

            foreach (var d in data)
            {
                dynamic stressed = CmuDictAsync(d.Value["name"].ToString());
                JObject JValue = d.Value as JObject;
                if (stressed == null)
                {
                    JValue.Add("stressed", null);
                    JValue.Add("pronunciation", null);
                }
                else
                {
                    JValue.Add("stressed", stressed);
                    dynamic pronunciation = Regex.Replace(stressed.ToString(), @"\d", ""); // 去掉音素中的数字
                    JValue.Add("pronunciation", pronunciation);
                }
            }
        }

        // CMUDdict 库文件解决的事
        // 传一个人英文名，找到这个英文名对应的第一个音素
        public dynamic CmuDictAsync(string lookUp)
        {
            if (cache.Cache.Count == 0) // 查看是否存在,存在直接读取无须下面的步骤
            {
                StreamReader bufferTemp = fs("cmudict.0.7a");
                char linesep = Convert.ToChar('\n');
                char comment = Convert.ToChar(';');

                dynamic strbuf = "";
                dynamic current = null;
                string buffer = bufferTemp.ReadToEnd(); // 读取每行数据
                foreach (var pos in buffer)
                {
                    current = pos;
                    if (current == linesep) // 如果是\n 表明这一行读取到尽头
                    {
                        if (strbuf[0] == comment) // 如果成立,表明当前行为;开头的无用行数
                        {
                            strbuf = "";
                            continue;
                        }
                        else
                        {
                            strbuf = strbuf.Split(new char[2] { ' ', ' ' });
                            cache.Cache.Add(strbuf[0], strbuf[2]);
                            strbuf = "";
                        }
                    }
                    else
                    {
                        strbuf += Convert.ToChar(current);
                    }
                }

            }
            return cache.Cache.GetValue(lookUp.ToUpper());
        }
    }

}
