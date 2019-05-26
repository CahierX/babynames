
#### 英文名
社会保障管理局的婴儿姓名数据分析
#### 数据
首先，您需要从社会安全管理局获取原始数据。并解压。这是下载地址[英文名数据](http://www.ssa.gov/OACT/babynames/names.zip)，因为为国外地址，需科学上网下载。
#### 如果使用
此为C# WebApi版本，您仅需下载运行即可
Node.js脚本版本传送门：[babynames](https://github.com/TimeMagazine/babynames)
运行程序后在浏览器上输入：[http://localhost:55651/api/Babynames/index](http://localhost:55651/api/Babynames/index)
##### 参数详解：
**store：** 默认为true，若主动传flase值，则程序会执行从txx——json的过程，然后生成json格式文件，因为txt内容由200w行，所有运行完大约需要花费40分钟。**强烈建议**生成一份json文件后之后操作采用stroe=false模式，此模式即从本地生成的json中操作，极大地解决了操作时间。`store=false`
**phonemes：** 默认为true，当为true时则生成phonemes文件，文件存储在/roster/下。`phonemes=false`
**name：** 只截取指定的name生成json文件。**注意：** 此参数尽在store=false时有效。`name=emma`
**start：** 生成json的开始日期，默认为1980。`start=1980`
**end：** 生成json的截止日期，默认为：2017。`end=2017`
**cutoff：** 获取至少出来了多少年的名字，默认不筛选 `cutoff=20`
**min：** 获取在一年内至少出现多少次的名字，默认不筛选 `min=10`
只传递一个参数时，用 **?** 接上参数 `http://localhost:55651/api/Babynames/index?store=true`
若需传递多个参数，用 **&**接上参数 `http://localhost:55651/api/Babynames/index?store=true&phonemes=false`

#### 建议
因为从社会保障管理局下载的数据一年只会改变一次，所有在第一次运行时建议使用 `http://localhost:55651/api/Babynames/index?store=false`完整运行一次生成flatTemp文件和数据，并将此文件更名为flat，之后如需传递其他参数如start、end、cutoff等只需`http://localhost:55651/api/Babynames/index?store=ture&cutoff=20` 即可，可大幅度节约时间。

#### 每年出生的婴儿总数
根据SSAextra/totals.json文件，其中包含每年出生的婴儿总数（或至少是发布SSN的婴儿）的数据。这个文件在计算每个英文名当年出现的百分比中用到，下载地址：[totals](http://www.ssa.gov/oact/babynames/numberUSbirths.html) 
###### 格式如下：
```
 "1880": {
    "year": 1880,
    "M": 118400,
    "F": 97605,
    "both": 216005
  },
  "1881": {
    "year": 1881,
    "M": 108282,
    "F": 98855,
    "both": 207137
  },
  "1882": {
    "year": 1882,
    "M": 122031,
    "F": 115695,
    "both": 237726
  },
```
###### 对应参数解析
这个文件很好理解，我们拿1880来做为例子，year是年份，M为1880年出现的所有英文名中男性的名字数量，F则为女性，both为男女总和，即1880年出现的所有英文名（应该没有中性人吧~）。所有这个文件的作用可想而知，当英文名每一年出现的次数除这个次数就得到了后面需要的percents值。
#### TXT -> JSON
在第三步下载的数据包解压后发现数据名为yob[year].txt，如：yob1880.txt，我们第一部是将这里的所有文件转换为json格式文件。
###### TXT数据格式
```
Mary,F,7065
Anna,F,2604
Emma,F,2003
Elizabeth,F,1939
Minnie,F,1746
Margaret,F,1578
Ida,F,1472
Alice,F,1414
Bertha,F,1320
Sarah,F,1288
Annie,F,1258
Clara,F,1226
Ella,F,1156
```
###### TXT文件分析
TXT文件很简单，第一列为英文名，第二列为姓名缩写，第三列为这个名称在文件名那年（文件名以年份命名）出现的次数。
###### 转换后的json格式
```
{
  "_id": "Aaban-M",
  "name": "Aaban",
  "gender": "M",
  "values": {
    "2007": 5,
    "2009": 6,
    "2010": 9,
    "2011": 11,
    "2012": 11,
    "2013": 14,
    "2014": 16,
    "2015": 15,
    "2016": 9,
    "2017": 11
  },
  "percents": {
    "2007": 0.0000022589446301045937,
    "2009": 0.0000028315871092940712,
    "2010": 0.0000043857041637875331,
    "2011": 0.0000054224882418272011,
    "2012": 0.0000054297123733272784,
    "2013": 0.0000069419273071123516,
    "2014": 0.0000078263413126143991,
    "2015": 0.0000073589618172907148,
    "2016": 0.0000044603254055179181,
    "2017": 0.0000056028401305971100
  },
  "normalized": {}
}
```
转换后的json文件储存在flat文件加下，文件命名方式为英文名-姓名.json，如：Aaban-M.json
###### json文件分析
**_id**：文件名
**name**：英文名
**gender**：性别缩写
**values**：此英文名在key年份对应出现的次数
**percents**：此英文名在key年份对应出现的百分比。这个值是通过前面介绍的totals文件里面的数据求出来的

#### 生成roster.json文件
```
["Mary-F","Anna-F","Emma-F","Elizabeth-F","Minnie-F","Margaret-F","Ida-F","Alice-F"]
```
此文件包换所有英文名，你可能会发现这里的每个英文名的命名方式刚好跟上面说到的_id一样，也和上面的json文件名一样。

#### 生成roster_short.json文件
```
["Mary (F)","Anna (F)","Emma (F)","Elizabeth (F)","Minnie (F)","Margaret (F)"]
```
这个文件里面的名字为：此名字即有男生也有女生

#### 生成phonemes.json文件
```
[
    {
        "phoneme":"M",
        "names":[
            "Mary",
            "Michael",
            "Manfredo"
        ],
        "percents":[
            {
                "key":"1880",
                "value":0.22621485420413504
            },
            {
                "key":"1881",
                "value":0.2206594444719741
            },
            {
                "key":"1882",
                "value":0.22384342845135538
            },
            {
                "key":"1883",
                "value":0.2200295824351297
            },
            {
                "key":"2016",
                "value":0.11625203529308542
            },
            {
                "key":"2017",
                "value":0.11543296533608732
            }
        ]
    }
]
```
省略部分数据，此文件是根据音素生成对应的英文名和对应的出现评率
**phoneme**：音素值
**names**：拥有此音素的英文名，注意这里面的英文名的出现顺序是按照每个英文名的peak值倒叙
**percents**：这个值为此音素在对应的年份出现的百分比，计算方式为：names中所有英文名在此年份的percents值累加。


