# PostProcessing

### 概述

本项目是一个简单的后处理解决方案，用于学习和总结现在的一些后处理技术。

### 后处理介绍

##### 轮廓线

使用Sobel算法实现，使用绝对值简化了其中的开方计算。

![Image](https://github.com/xlxlzh/PostProcessing/raw/master/Images/original.png)

![Image](https://github.com/xlxlzh/PostProcessing/raw/master/Images/edge.png)

#### 后处理雾效

一种基于传统雾效算法实现的后处理雾，支持linear、exp、exp2等三种模式，暂时不支持高度上的渐变。

- Fog Mode  雾效的模式
- Fog Color 雾效的颜色
- Fog Start 雾效的开始距离，基于Camera Space的距离，exp exp2模式不使用该值
- Fog End 雾效权重为1时的距离，基于Camera Space的距离，exp exp2模式不使用该值
- Fog Intensity 雾的密度，linear模式不使用该值

![Image](https://github.com/xlxlzh/PostProcessing/raw/master/Images/fog.png)