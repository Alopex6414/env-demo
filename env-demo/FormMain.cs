using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

namespace env_demo
{
    public partial class FormMain : Form
    {
        #region 导入设备动态链接库函数
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DEVICE_MSG_CALLBACK(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser); // 声明回调函数委托

        [DllImport("SenserDLL.dll", EntryPoint = "SD_InitSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SD_InitSDK(); // 初始化SDK

        [DllImport("SenserDLL.dll", EntryPoint = "SD_ReleaseSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SD_ReleaseSDK();  // 释放SDK

        [DllImport("SenserDLL.dll", EntryPoint = "SD_SetIpAndPort", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int SD_SetIpAndPort(string sHostIp, int nHostPort);  // 设置本机IP地址和端口

        [DllImport("SenserDLL.dll", EntryPoint = "SD_SetDeviceMessageCallback", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int SD_SetDeviceMessageCallback(DEVICE_MSG_CALLBACK pfCB, IntPtr pUser);   // 设置消息回调

        [DllImport("SenserDLL.dll", EntryPoint = "SD_LoginDevice", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern long SD_LoginDevice(string sDevIp, int nDevPort, string sUserName, string sUserPassword);  // 设备登录

        [DllImport("SenserDLL.dll", EntryPoint = "SD_LogoutDevice", CallingConvention = CallingConvention.Cdecl)]
        public static extern long SD_LogoutDevice(Int32 lDevHandle);  // 设备登出

        [DllImport("SenserDLL.dll", EntryPoint = "SD_LogoutDeviceAll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SD_LogoutDeviceAll();  // 设备全部登出

        [DllImport("SenserDLL.dll", EntryPoint = "SD_DeleteDeviceAll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SD_DeleteDeviceAll();  // 设备全部删除

        [DllImport("SenserDLL.dll", EntryPoint = "SD_GetDeviceInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool SD_GetDeviceInfo(Int32 lDevHandle, Int32 nMsgType, IntPtr pSendBuffer, Int32 nSendSize, IntPtr pRecvBuffer, Int32 nRecvSize);  // 获取设备状态信息

        [DllImport("SenserDLL.dll", EntryPoint = "SD_SetDeviceInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool SD_SetDeviceInfo(Int32 lDevHandle, Int32 nMsgType, IntPtr pSendBuffer, Int32 nSendSize, IntPtr pRecvBuffer, Int32 nRecvSize);  // 设置设备状态信息
        #endregion

        #region 变量定义
        DEVICE_MSG_CALLBACK fDeviceMsgCallback = new DEVICE_MSG_CALLBACK(FormMain_DeviceMessageCallback);

        public static FormMain m_sInstance;
        public int m_nDevHandle = -1;

        #endregion

        #region 结构定义
        // 设备状态结构体(21/22)
        struct MSG_DEVICE_STATUS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] nUSBStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] nInputStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] nOutputStatus;
            public byte nPowerInputStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] nPowerOutputAStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] nPowerOutputVStatus;
            public byte nPower12VStatus;
            public byte nElectricStatus;
            public byte nWaterLeachStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] nTemStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] nHumStatus;
            public byte nWireTemStatus;
        };

        // 设备推送频率结构体(24/25)
        struct MSG_DEVICE_FREQ
        {
            public Int16 nDeviceFreq;
        };

        // 市电状态推送频率结构体(31/32)
        struct MSG_ELECTRIC_FREQ
        {
            public Int16 nElectricFreq;
        };

        // 电源输出获取结构体(41/43/45/49)
        struct MSG_POWEROUT_GET_EX
        {
            public byte nPort;
        };

        // 电源输出监测频率结构体(41/42)
        struct MSG_POWEROUT_FREQ
        {
            public byte nPort;
            public Int16 nPowerOutputFreq;
        };

        // 电源输出安全阈值结构体(43/44)
        struct MSG_POWEROUT_SAFEVALUE
        {
            public byte nPort;
            public byte nCurrentSafeHigh;
            public byte nCurrentSafeLow;
            public byte nVoltageSafeHigh;
            public byte nVoltageSafeLow;
        };

        // 电源输出获取值结构体(45/46)
        struct MSG_POWEROUT_VALUE
        {
            public byte nPort;
            public Int16 nCurrent;
            public Int16 nVoltage;
        };

        // 电源输出状态控制结构体(48/49)
        struct MSG_POWEROUT_STATUS
        {
            public byte nPort;
            public byte nStatus;
        };

        // 温湿度获取结构体(75/77)
        struct MSG_TEMANDHUM_GET_EX
        {
            public byte nPort;
            public byte nID;
        };

        // 温湿度状态监测频率结构体(73/74)
        struct MSG_TEMANDHUM_FREQ
        {
            public Int16 nTemAndHumFreq;
        };

        // 温湿度安全阈值结构体(75/76)
        struct MSG_TEMANDHUM_SAFEVALUE
        {
            public byte nPort;     
            public byte nID;
            public Int16 nTemAndHumSafeValueHigh;
            public Int16 nTemAndHumSafeValueLow;
        };

        // 温湿度值结构体(77/78)
        struct MSG_TEMANDHUM_VALUE
        {
            public byte nPort;
            public byte nID;
            public Int16 nTemAndHumValue;
        };

        // IO输出状态结构体(91/92/93)
        struct MSG_OUTPUT_STATUS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] nStatus;
        };

        // 电线温度监测频率结构体(111/112)
        struct MSG_WIRE_FREQ
        {
            public Int16 nWireTemFreq;
        };

        // 电线温度安全阈值结构体(113/114)
        struct MSG_WIRE_SAFEVALUE
        {
            public Int16 nWireTemSafeValueHigh;
            public Int16 nWireTemSafeValueLow;
        };

        // 电线温度值结构体(115/116)
        struct MSG_WIRE_VALUE
        {
            public Int16 nWireTemValue;
        };

        // 水浸使能开关状态结构体(61/62)
        struct MSG_WATERLEACH_ENABLE
        {
            public byte nStatus;
        };

        // 温湿度使能开关状态结构体(71/72)
        struct MSG_TEMANDHUM_ENABLE
        {
            public byte nStatus1;
            public byte nStatus2;
        };

        // IO输入使能状态结构体(81/82)
        struct MSG_INPUT_ENABLE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] nStatus;
        };

        // IO输入电平反转结构体(83)
        struct MSG_INPUT_TURN
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] nStatus;
        };

        // USB使能状态结构体(101/102)
        struct MSG_USB_ENABLE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] nStatus;
        };

        // 要求反馈结构体(131)
        struct MSG_CALLBACK
        {
            public byte nStatus;
        };

        #endregion

        #region 窗体初始化
        public FormMain()   // 窗体初始化
        {
            InitializeComponent();
        }
        #endregion

        #region 窗体初始化加载
        private void FormMain_Load(object sender, EventArgs e)  // 窗体初始化加载
        {
            // 窗体静态成员
            m_sInstance = this;

            // 窗体属性设置
            this.MaximizeBox = false;   // 窗体最大化按钮关闭
            this.MinimizeBox = true;    // 窗体最小化按钮开启
            this.DoubleBuffered = true; // 窗体双缓冲开启
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // 窗体边框固定的单行边框
            this.StartPosition = FormStartPosition.CenterScreen;    // 窗体居中屏幕

            // 控件属性设置
            // 登录窗体
            TabLogin_TextBox_DeviceIP.MaxLength = 16;
            TabLogin_TextBox_DevicePort.MaxLength = 16;
            TabLogin_TextBox_HostIP.MaxLength = 16;
            TabLogin_TextBox_HostPort.MaxLength = 16;


            // 设备SDK相关初始化
            InitDevSDK();   // 初始化设备SDK
            SetDevMsgCallback(); // 设置消息回调函数

        }
        #endregion

        #region 窗体关闭退出
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 设备SDK相关释放
            ReleaseDevSDK();    // 释放SDK
        }
        #endregion

        #region 窗体功能函数
        private bool IsIPAddress(string sIpAddress) // IP地址是否合法
        {
            bool blnTest = false;
            bool bRet = true;

            Regex regex = new Regex("^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$");
            blnTest = regex.IsMatch(sIpAddress);
            if (blnTest == true)
            {
                string[] strTemp = sIpAddress.Split(new char[] { '.' });
                int nDotCount = strTemp.Length - 1;
                if (3 == nDotCount)
                {
                    for (int i = 0; i < strTemp.Length; i++)
                    {
                        if (Convert.ToInt32(strTemp[i]) > 255)
                        {
                            bRet = false;
                        }
                    }
                }
                else
                {
                    bRet = false;
                }
            }
            else
            {
                bRet = false;
            }
            return bRet;
        }

        private delegate void TabConsoleWriteDelegate(string str);

        public void TabConsoleWrite(string str) // 控制台输出调试信息
        {
            if(!TabConsole_TextBox_Console.InvokeRequired)
            {
                string strArr = string.Empty;

                strArr += "[";
                strArr += DateTime.Now.ToString();
                strArr += "]";
                strArr += str;

                TabConsole_TextBox_Console.AppendText(strArr);
            }
            else
            {
                TabConsoleWriteDelegate tabConsoleWrite = new TabConsoleWriteDelegate(TabConsoleWrite);
                TabConsole_TextBox_Console.Invoke(tabConsoleWrite, str);
            }
            
        }

        #endregion

        #region 设备相关函数
        private void InitDevSDK()   // 初始化设备SDK
        {
            int nRet = 0;

            nRet = SD_InitSDK();    // 初始化SDK
            if(0 != nRet)
            {
                MessageBox.Show("初始化设备SDK失败", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }

            TabConsoleWrite("初始化设备SDK成功!\n");
        }

        private void SetDevMsgCallback()    // 设置设备消息回调函数
        {
            int nRet = 0;

            nRet = SD_SetDeviceMessageCallback(fDeviceMsgCallback, (IntPtr)0);
            if(0 != nRet)
            {
                MessageBox.Show("设置设备消息回调失败", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }

            TabConsoleWrite("设置设备消息回调成功!\n");
        }

        private void SetIpAndPort(string sHostIp, int nHostPort) // 设置本机IP地址和端口号
        {
            int nRet = 0;

            nRet = SD_SetIpAndPort(sHostIp, nHostPort);
            if (0 != nRet)
            {
                MessageBox.Show("设置本机IP地址失败", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TabConsoleWrite("设置本机IP地址失败!\n");
                return;
            }

            TabConsoleWrite("设置本机IP地址成功!\n");
        }

        private int LoginDevice(string sDevIp, int nDevPort)   // 设备登录
        {
            int lDevHandle = 0;

            lDevHandle = (int)SD_LoginDevice(sDevIp, nDevPort, "admin", "admin");
            if(-1 == lDevHandle)
            {
                MessageBox.Show("设备登录失败", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TabConsoleWrite("设备登录失败!\n");
                return -1;
            }

            TabConsoleWrite("设备登录成功!\n");
            return lDevHandle;
        }

        private void DeleteDeviceAll()  // 设备删除
        {
            SD_DeleteDeviceAll();
            TabConsoleWrite("设备断开成功!\n");
        }

        private void ReleaseDevSDK()    // 释放设备SDK
        {
            int nRet = 0;

            nRet = SD_ReleaseSDK();    // 释放SDK
            if (0 != nRet)
            {
                MessageBox.Show("释放设备SDK失败", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }

            TabConsoleWrite("释放设备SDK成功!\n");
        }

        #endregion

        #region 设备报警相关回调函数
        private void HandleDeviceOfflineProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser) // 设备掉线处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:设备离线.\n");
        }

        private void HandlePushDeviceStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser) // 主动推送设备状态处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:主动推送设备状态.\n");
        }

        private void HandleHeartDeviceStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser) // 心跳推送设备状态处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:心跳推送设备状态.\n");
        }

        private void HandlePushElectricStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)   // 主动推送市电状态报警函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:主动推送市电状态.\n");
        }

        private void HandleAlarmElectricStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)   // 推送市电断电报警处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:推送市电状态报警.\n");
        }

        private void HandlePushPowerOutputStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)   // 推送电源输出报警处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:主动推送电源输出状态.\n");
        }

        private void HandleAlarmPowerOutputStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)   // 电源输出过载报警处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:推送电源输出状态报警.\n");
        }

        private void HandleAlarmWaterLeachStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser) // 水浸状态报警处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:推送水浸状态报警.\n");
        }

        private void HandlePushTemAndHumStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)   // 主动推送温湿度状态处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:主动推送温湿度状态.\n");
        }

        private void HandleAlarmTemAndHumStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)  // 温湿度过载报警处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:推送温湿度状态报警.\n");
        }

        private void HandlePushInputStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)  // 主动推送IO输入状态处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:主动推送IO输入状态.\n");
        }

        private void HandleAlarmInputStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)  // IO输入异常报警处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:推送IO输入状态报警.\n");
        }

        private void HandleAlarmUSBStatusProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)    // USB异常状态推送处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:推送USB状态报警.\n");
        }

        private void HandlePushWireTemValueProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)    // 主动推送电线温度处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:主动推送电线温度状态.\n");
        }

        private void HandleAlarmWireTemValueProcess(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)    // 电线温度过载报警处理函数
        {
            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
            m_sInstance.TabConsoleWrite("client<-dev:推送电线温度状态报警.\n");
        }

        #endregion

        #region 设备访问相关函数
        private void GetDeviceFreqMsgAll()
        {
            bool bRet = false;
            MSG_DEVICE_FREQ sDeviceGetInfo = new MSG_DEVICE_FREQ();

            sDeviceGetInfo.nDeviceFreq = 0;

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取设备状态推送频率
            bRet = SD_GetDeviceInfo(m_nDevHandle, 24, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if (!bRet)
            {
                MessageBox.Show("获取设备状态推送频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 24));
            m_sInstance.TabConsoleWrite("client->dev:获取设备状态推送频率.\n");

            MSG_DEVICE_FREQ sGetInfo = (MSG_DEVICE_FREQ)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_DEVICE_FREQ));

            // 温湿度推送频率
            int nVar = 0;
            string strValue = "";

            nVar = sGetInfo.nDeviceFreq;
            strValue = string.Format("{0}s", nVar);
            TabAccess_TextBox_Dev_Freq.Text = strValue;
        }

        private void GetElectricFreqMsgAll()
        {
            bool bRet = false;
            MSG_ELECTRIC_FREQ sDeviceGetInfo = new MSG_ELECTRIC_FREQ();

            sDeviceGetInfo.nElectricFreq = 0;

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取市电推送频率
            bRet = SD_GetDeviceInfo(m_nDevHandle, 31, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if (!bRet)
            {
                MessageBox.Show("获取市电推送频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 31));
            m_sInstance.TabConsoleWrite("client->dev:获取市电推送频率.\n");

            MSG_ELECTRIC_FREQ sGetInfo = (MSG_ELECTRIC_FREQ)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_ELECTRIC_FREQ));

            // 温湿度推送频率
            int nVar = 0;
            string strValue = "";

            nVar = sGetInfo.nElectricFreq;
            strValue = string.Format("{0}s", nVar);
            TabAccess_TextBox_Elec_Freq.Text = strValue;
        }

        private bool GetPowerOutputValueMsg(byte nPort)
        {
            bool bRet = false;
            MSG_POWEROUT_GET_EX sTransInfo = new MSG_POWEROUT_GET_EX();
            MSG_POWEROUT_VALUE sDeviceGetInfo = new MSG_POWEROUT_VALUE();

            sTransInfo.nPort = nPort;
            int nTSize = Marshal.SizeOf(sTransInfo);
            IntPtr pTransInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sTransInfo, pTransInfo, true);

            sDeviceGetInfo.nPort = 0;
            sDeviceGetInfo.nCurrent = 0;
            sDeviceGetInfo.nVoltage = 0;

            int nRSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取电源输出值
            bRet = SD_GetDeviceInfo(m_nDevHandle, 45, pTransInfo, nTSize, pDeviceGetInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("获取电源输出状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 45));
            m_sInstance.TabConsoleWrite("client->dev:获取电源输出状态.\n");

            MSG_POWEROUT_VALUE sGetInfo = (MSG_POWEROUT_VALUE)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_POWEROUT_VALUE));

            if (sGetInfo.nPort == 1)
            {
                float fVarA = 0.0f;
                float fVarV = 0.0f;
                float fVarW = 0.0f;
                string strVarA = "";
                string strVarV = "";
                string strVarW = "";

                fVarA = sGetInfo.nCurrent * 0.1f;
                fVarV = sGetInfo.nVoltage;
                fVarW = fVarA * fVarV;
                strVarA = string.Format("{0:F1}A", fVarA);
                strVarV = string.Format("{0:D}V", (int)fVarV);
                strVarW = string.Format("{0:F}W", fVarW);
                TabAccess_TextBox_Power1_ValueA.Text = strVarA;
                TabAccess_TextBox_Power1_ValueV.Text = strVarV;
                TabAccess_TextBox_Power1_ValueW.Text = strVarW;
            }
            else if (sGetInfo.nPort == 2)
            {
                float fVarA = 0.0f;
                float fVarV = 0.0f;
                float fVarW = 0.0f;
                string strVarA = "";
                string strVarV = "";
                string strVarW = "";

                fVarA = sGetInfo.nCurrent * 0.1f;
                fVarV = sGetInfo.nVoltage;
                fVarW = fVarA * fVarV;
                strVarA = string.Format("{0:F1}A", fVarA);
                strVarV = string.Format("{0:D}V", (int)fVarV);
                strVarW = string.Format("{0:F}W", fVarW);
                TabAccess_TextBox_Power2_ValueA.Text = strVarA;
                TabAccess_TextBox_Power2_ValueV.Text = strVarV;
                TabAccess_TextBox_Power2_ValueW.Text = strVarW;
            }
            else if (sGetInfo.nPort == 3)
            {
                float fVarA = 0.0f;
                float fVarV = 0.0f;
                float fVarW = 0.0f;
                string strVarA = "";
                string strVarV = "";
                string strVarW = "";

                fVarA = sGetInfo.nCurrent * 0.1f;
                fVarV = sGetInfo.nVoltage;
                fVarW = fVarA * fVarV;
                strVarA = string.Format("{0:F1}A", fVarA);
                strVarV = string.Format("{0:D}V", (int)fVarV);
                strVarW = string.Format("{0:F}W", fVarW);
                TabAccess_TextBox_Power3_ValueA.Text = strVarA;
                TabAccess_TextBox_Power3_ValueV.Text = strVarV;
                TabAccess_TextBox_Power3_ValueW.Text = strVarW;
            }

            return true;
        }

        private void GetPowerOutputValueMsgAll()
        {
            for (int i = 0; i < 3; ++i)
            {
                GetPowerOutputValueMsg((byte)(i + 1));
            }

        }

        private bool GetPowerOutputSafeValueMsg(byte nPort)
        {
            bool bRet = false;
            MSG_POWEROUT_GET_EX sTransInfo = new MSG_POWEROUT_GET_EX();
            MSG_POWEROUT_SAFEVALUE sDeviceGetInfo = new MSG_POWEROUT_SAFEVALUE();

            sTransInfo.nPort = nPort;
            int nTSize = Marshal.SizeOf(sTransInfo);
            IntPtr pTransInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sTransInfo, pTransInfo, true);

            sDeviceGetInfo.nPort = 0;
            sDeviceGetInfo.nCurrentSafeLow = 0;
            sDeviceGetInfo.nCurrentSafeHigh = 0;
            sDeviceGetInfo.nVoltageSafeLow = 0;
            sDeviceGetInfo.nVoltageSafeHigh = 0;

            int nRSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取电源安全阈值
            bRet = SD_GetDeviceInfo(m_nDevHandle, 43, pTransInfo, nTSize, pDeviceGetInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("获取电源输出安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 43));
            m_sInstance.TabConsoleWrite("client->dev:获取电源安全阈值.\n");

            MSG_POWEROUT_SAFEVALUE sGetInfo = (MSG_POWEROUT_SAFEVALUE)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_POWEROUT_SAFEVALUE));

            if (sGetInfo.nPort == 1)
            {
                float fVarA = 0.0f;
                float fVarVL = 0.0f;
                float fVarVH = 0.0f;
                string strVarA = "";
                string strVarVL = "";
                string strVarVH = "";

                fVarA = sGetInfo.nCurrentSafeHigh * 0.1f;
                fVarVL = sGetInfo.nVoltageSafeLow;
                fVarVH = sGetInfo.nVoltageSafeHigh;
                strVarA = string.Format("{0:F1}A", fVarA);
                strVarVL = string.Format("{0:D}V", (int)fVarVL);
                strVarVH = string.Format("{0:D}V", (int)fVarVH);
                TabAccess_TextBox_Power1_SafeValueA.Text = strVarA;
                TabAccess_TextBox_Power1_SafeValueVL.Text = strVarVL;
                TabAccess_TextBox_Power1_SafeValueVH.Text = strVarVH;
            }
            else if (sGetInfo.nPort == 2)
            {
                float fVarA = 0.0f;
                float fVarVL = 0.0f;
                float fVarVH = 0.0f;
                string strVarA = "";
                string strVarVL = "";
                string strVarVH = "";

                fVarA = sGetInfo.nCurrentSafeHigh * 0.1f;
                fVarVL = sGetInfo.nVoltageSafeLow;
                fVarVH = sGetInfo.nVoltageSafeHigh;
                strVarA = string.Format("{0:F1}A", fVarA);
                strVarVL = string.Format("{0:D}V", (int)fVarVL);
                strVarVH = string.Format("{0:D}V", (int)fVarVH);
                TabAccess_TextBox_Power2_SafeValueA.Text = strVarA;
                TabAccess_TextBox_Power2_SafeValueVL.Text = strVarVL;
                TabAccess_TextBox_Power2_SafeValueVH.Text = strVarVH;
            }
            else if (sGetInfo.nPort == 3)
            {
                float fVarA = 0.0f;
                float fVarVL = 0.0f;
                float fVarVH = 0.0f;
                string strVarA = "";
                string strVarVL = "";
                string strVarVH = "";

                fVarA = sGetInfo.nCurrentSafeHigh * 0.1f;
                fVarVL = sGetInfo.nVoltageSafeLow;
                fVarVH = sGetInfo.nVoltageSafeHigh;
                strVarA = string.Format("{0:F1}A", fVarA);
                strVarVL = string.Format("{0:D}V", (int)fVarVL);
                strVarVH = string.Format("{0:D}V", (int)fVarVH);
                TabAccess_TextBox_Power3_SafeValueA.Text = strVarA;
                TabAccess_TextBox_Power3_SafeValueVL.Text = strVarVL;
                TabAccess_TextBox_Power3_SafeValueVH.Text = strVarVH;
            }

            return true;
        }

        private void GetPowerOutputSafeValueMsgAll()
        {
            for (int i = 0; i < 3; ++i)
            {
                GetPowerOutputSafeValueMsg((byte)(i + 1));
            }

        }

        private bool GetPowerOutputFreqMsg(byte nPort)
        {
            bool bRet = false;
            MSG_POWEROUT_GET_EX sTransInfo = new MSG_POWEROUT_GET_EX();
            MSG_POWEROUT_FREQ sDeviceGetInfo = new MSG_POWEROUT_FREQ();

            sTransInfo.nPort = nPort;
            int nTSize = Marshal.SizeOf(sTransInfo);
            IntPtr pTransInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sTransInfo, pTransInfo, true);

            sDeviceGetInfo.nPort = 0;
            sDeviceGetInfo.nPowerOutputFreq = 0;

            int nRSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取电源输出监测频率
            bRet = SD_GetDeviceInfo(m_nDevHandle, 41, pTransInfo, nTSize, pDeviceGetInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("获取电源输出监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 41));
            m_sInstance.TabConsoleWrite("client->dev:获取电源输出监测频率.\n");

            MSG_POWEROUT_FREQ sGetInfo = (MSG_POWEROUT_FREQ)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_POWEROUT_FREQ));

            if (sGetInfo.nPort == 1)
            {
                int nFreq = 0;
                string strFreq = "";

                nFreq = sGetInfo.nPowerOutputFreq;
                strFreq = string.Format("{0:D}s", nFreq);
                TabAccess_TextBox_Power1_Freq.Text = strFreq;
            }
            else if (sGetInfo.nPort == 2)
            {
                int nFreq = 0;
                string strFreq = "";

                nFreq = sGetInfo.nPowerOutputFreq;
                strFreq = string.Format("{0:D}s", nFreq);
                TabAccess_TextBox_Power2_Freq.Text = strFreq;
            }
            else if (sGetInfo.nPort == 3)
            {
                int nFreq = 0;
                string strFreq = "";

                nFreq = sGetInfo.nPowerOutputFreq;
                strFreq = string.Format("{0:D}s", nFreq);
                TabAccess_TextBox_Power3_Freq.Text = strFreq;
            }

            return true;
        }

        private void GetPowerOutputFreqMsgAll()
        {
            for (int i = 0; i < 3; ++i)
            {
                GetPowerOutputFreqMsg((byte)(i + 1));
            }

        }

        private bool GetPowerOutputSwitchMsg(byte nPort)
        {
            bool bRet = false;
            MSG_POWEROUT_GET_EX sTransInfo = new MSG_POWEROUT_GET_EX();
            MSG_POWEROUT_STATUS sDeviceGetInfo = new MSG_POWEROUT_STATUS();

            sTransInfo.nPort = nPort;
            int nTSize = Marshal.SizeOf(sTransInfo);
            IntPtr pTransInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sTransInfo, pTransInfo, true);

            sDeviceGetInfo.nPort = 0;
            sDeviceGetInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取电源输出开关状态
            bRet = SD_GetDeviceInfo(m_nDevHandle, 49, pTransInfo, nTSize, pDeviceGetInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("获取电源输出开关状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 49));
            m_sInstance.TabConsoleWrite("client->dev:获取电源输出开关状态.\n");

            MSG_POWEROUT_STATUS sGetInfo = (MSG_POWEROUT_STATUS)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_POWEROUT_STATUS));

            if (sGetInfo.nPort == 1)
            {
                int nStatus = 0;
                string strStatus = "";

                nStatus = sGetInfo.nStatus;
                if (nStatus == 0)
                {
                    strStatus = "关闭";
                    TabAccess_TextBox_Power1_Switch.BackColor = Color.Orange;
                }
                else
                {
                    strStatus = "开启";
                    TabAccess_TextBox_Power1_Switch.BackColor = Color.Azure;
                }

                TabAccess_TextBox_Power1_Switch.Text = strStatus;
            }
            else if (sGetInfo.nPort == 2)
            {
                int nStatus = 0;
                string strStatus = "";

                nStatus = sGetInfo.nStatus;
                if (nStatus == 0)
                {
                    strStatus = "关闭";
                    TabAccess_TextBox_Power2_Switch.BackColor = Color.Orange;
                }
                else
                {
                    strStatus = "开启";
                    TabAccess_TextBox_Power2_Switch.BackColor = Color.Azure;
                }

                TabAccess_TextBox_Power2_Switch.Text = strStatus;
            }
            else if (sGetInfo.nPort == 3)
            {
                int nStatus = 0;
                string strStatus = "";

                nStatus = sGetInfo.nStatus;
                if (nStatus == 0)
                {
                    strStatus = "关闭";
                    TabAccess_TextBox_Power3_Switch.BackColor = Color.Orange;
                }
                else
                {
                    strStatus = "开启";
                    TabAccess_TextBox_Power3_Switch.BackColor = Color.Azure;
                }

                TabAccess_TextBox_Power3_Switch.Text = strStatus;
            }

            return true;
        }

        private void GetPowerOutputSwitchMsgAll()
        {
            for (int i = 0; i < 3; ++i)
            {
                GetPowerOutputSwitchMsg((byte)(i + 1));
            }

        }

        private bool GetTemAndHumValueMsg(byte nPort, byte nID)
        {
            bool bRet = false;
            MSG_TEMANDHUM_GET_EX sTransInfo = new MSG_TEMANDHUM_GET_EX();
            MSG_TEMANDHUM_VALUE sDeviceGetInfo = new MSG_TEMANDHUM_VALUE();

            sTransInfo.nPort = nPort;
            sTransInfo.nID = nID;
            int nTSize = Marshal.SizeOf(sTransInfo);
            IntPtr pTransInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sTransInfo, pTransInfo, true);

            sDeviceGetInfo.nPort = 0;
            sDeviceGetInfo.nID = 0;
            sDeviceGetInfo.nTemAndHumValue = 0;

            int nRSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取温湿度值
            bRet = SD_GetDeviceInfo(m_nDevHandle, 77, pTransInfo, nTSize, pDeviceGetInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("获取温湿度状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 77));
            m_sInstance.TabConsoleWrite("client->dev:获取温湿度状态.\n");

            MSG_TEMANDHUM_VALUE sGetInfo = (MSG_TEMANDHUM_VALUE)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_TEMANDHUM_VALUE));

            if (sGetInfo.nPort == 1 && sGetInfo.nID == 1)
            {
                // 温度1
                float fValue = 0.0f;
                string strValue = "";

                fValue = sGetInfo.nTemAndHumValue * 0.1f - 40.0f;
                strValue = string.Format("{0:F}℃", fValue);
                TabAccess_TextBox_Tem1_Value.Text = strValue;
            }
            else if (sGetInfo.nPort == 1 && sGetInfo.nID == 2)
            {
                // 湿度1
                float fValue = 0.0f;
                string strValue = "";

                fValue = sGetInfo.nTemAndHumValue * 0.1f;
                strValue = string.Format("{0:F}%", fValue);
                TabAccess_TextBox_Hum1_Value.Text = strValue;
            }
            else if (sGetInfo.nPort == 2 && sGetInfo.nID == 1)
            {
                // 温度2
                float fValue = 0.0f;
                string strValue = "";

                fValue = sGetInfo.nTemAndHumValue * 0.1f - 40.0f;
                strValue = string.Format("{0:F}℃", fValue);
                TabAccess_TextBox_Tem2_Value.Text = strValue;
            }
            else if (sGetInfo.nPort == 2 && sGetInfo.nID == 2)
            {
                // 湿度2
                float fValue = 0.0f;
                string strValue = "";

                fValue = sGetInfo.nTemAndHumValue * 0.1f;
                strValue = string.Format("{0:F}%", fValue);
                TabAccess_TextBox_Hum2_Value.Text = strValue;
            }

            return true;
        }

        private void GetTemAndHumValueMsgAll()
        {
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < 2; ++j)
                {
                    GetTemAndHumValueMsg((byte)(i + 1), (byte)(j + 1));
                }
            }

        }

        private bool GetTemAndHumSafeValueMsg(byte nPort, byte nID)
        {
            bool bRet = false;
            MSG_TEMANDHUM_GET_EX sTransInfo = new MSG_TEMANDHUM_GET_EX();
            MSG_TEMANDHUM_SAFEVALUE sDeviceGetInfo = new MSG_TEMANDHUM_SAFEVALUE();

            sTransInfo.nPort = nPort;
            sTransInfo.nID = nID;
            int nTSize = Marshal.SizeOf(sTransInfo);
            IntPtr pTransInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sTransInfo, pTransInfo, true);

            sDeviceGetInfo.nID = 0;
            sDeviceGetInfo.nPort = 0;
            sDeviceGetInfo.nTemAndHumSafeValueLow = 0;
            sDeviceGetInfo.nTemAndHumSafeValueHigh = 0;

            int nRSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取温湿度安全阈值
            bRet = SD_GetDeviceInfo(m_nDevHandle, 75, pTransInfo, nTSize, pDeviceGetInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("获取温湿度安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 75));
            m_sInstance.TabConsoleWrite("client->dev:获取温湿度安全阈值.\n");

            MSG_TEMANDHUM_SAFEVALUE sGetInfo = (MSG_TEMANDHUM_SAFEVALUE)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_TEMANDHUM_SAFEVALUE));

            if (sGetInfo.nPort == 1 && sGetInfo.nID == 1)
            {
                // 温度1
                float fValueL = 0.0f;
                float fValueH = 0.0f;
                string strValueL = "";
                string strValueH = "";

                fValueL = sGetInfo.nTemAndHumSafeValueLow * 0.1f - 40.0f;
                fValueH = sGetInfo.nTemAndHumSafeValueHigh * 0.1f - 40.0f;
                strValueL = string.Format("{0:D}℃", (int)fValueL);
                strValueH = string.Format("{0:D}℃", (int)fValueH);
                TabAccess_TextBox_Tem1_SafeValue_L.Text = strValueL;
                TabAccess_TextBox_Tem1_SafeValue_H.Text = strValueH;
            }
            else if (sGetInfo.nPort == 1 && sGetInfo.nID == 2)
            {
                // 湿度1
                float fValueL = 0.0f;
                float fValueH = 0.0f;
                string strValueL = "";
                string strValueH = "";

                fValueL = sGetInfo.nTemAndHumSafeValueLow * 0.1f;
                fValueH = sGetInfo.nTemAndHumSafeValueHigh * 0.1f;
                strValueL = string.Format("{0:D}%", (int)fValueL);
                strValueH = string.Format("{0:D}%", (int)fValueH);
                TabAccess_TextBox_Hum1_SafeValue_L.Text = strValueL;
                TabAccess_TextBox_Hum1_SafeValue_H.Text = strValueH;
            }
            else if (sGetInfo.nPort == 2 && sGetInfo.nID == 1)
            {
                // 温度2
                float fValueL = 0.0f;
                float fValueH = 0.0f;
                string strValueL = "";
                string strValueH = "";

                fValueL = sGetInfo.nTemAndHumSafeValueLow * 0.1f - 40.0f;
                fValueH = sGetInfo.nTemAndHumSafeValueHigh * 0.1f - 40.0f;
                strValueL = string.Format("{0:D}℃", (int)fValueL);
                strValueH = string.Format("{0:D}℃", (int)fValueH);
                TabAccess_TextBox_Tem2_SafeValue_L.Text = strValueL;
                TabAccess_TextBox_Tem2_SafeValue_H.Text = strValueH;
            }
            else if (sGetInfo.nPort == 2 && sGetInfo.nID == 2)
            {
                // 湿度2
                float fValueL = 0.0f;
                float fValueH = 0.0f;
                string strValueL = "";
                string strValueH = "";

                fValueL = sGetInfo.nTemAndHumSafeValueLow * 0.1f;
                fValueH = sGetInfo.nTemAndHumSafeValueHigh * 0.1f;
                strValueL = string.Format("{0:D}%", (int)fValueL);
                strValueH = string.Format("{0:D}%", (int)fValueH);
                TabAccess_TextBox_Hum2_SafeValue_L.Text = strValueL;
                TabAccess_TextBox_Hum2_SafeValue_H.Text = strValueH;
            }

            return true;
        }

        private void GetTemAndHumSafeValueMsgAll()
        {
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < 2; ++j)
                {
                    GetTemAndHumSafeValueMsg((byte)(i + 1), (byte)(j + 1));
                }
            }
        }

        private void GetTemAndHumFreqMsgAll()
        {
            bool bRet = false;
            MSG_TEMANDHUM_FREQ sDeviceGetInfo = new MSG_TEMANDHUM_FREQ();

            sDeviceGetInfo.nTemAndHumFreq = 0;

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取温湿度推送频率
            bRet = SD_GetDeviceInfo(m_nDevHandle, 73, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if (!bRet)
            {
                MessageBox.Show("获取温湿度推送频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 73));
            m_sInstance.TabConsoleWrite("client->dev:获取温湿度推送频率.\n");

            MSG_TEMANDHUM_FREQ sGetInfo = (MSG_TEMANDHUM_FREQ)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_TEMANDHUM_FREQ));

            // 温湿度推送频率
            int nVar = 0;
            string strValue = "";

            nVar = sGetInfo.nTemAndHumFreq;
            strValue = string.Format("{0}s", nVar);
            TabAccess_TextBox_Tem1_Freq.Text = strValue;
            TabAccess_TextBox_Tem2_Freq.Text = strValue;
            TabAccess_TextBox_Hum1_Freq.Text = strValue;
            TabAccess_TextBox_Hum2_Freq.Text = strValue;
        }

        private bool GetWireTemValueMsg()
        {
            bool bRet = false;
            MSG_WIRE_VALUE sDeviceGetInfo = new MSG_WIRE_VALUE();

            sDeviceGetInfo.nWireTemValue = 0;

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取电线温度值
            bRet = SD_GetDeviceInfo(m_nDevHandle, 115, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if (!bRet)
            {
                MessageBox.Show("获取电线温度状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 115));
            m_sInstance.TabConsoleWrite("client->dev:获取电线温度状态.\n");

            MSG_WIRE_VALUE sGetInfo = (MSG_WIRE_VALUE)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_WIRE_VALUE));

            // 电线温度
            float fValue = 0.0f;
            string strValue = "";

            fValue = sGetInfo.nWireTemValue * 0.1f - 40.0f;
            strValue = string.Format("{0:F}℃", fValue);
            TabAccess_TextBox_Wire_Value.Text = strValue;

            return true;
        }

        private void GetWireTemValueMsgAll()
        {
            GetWireTemValueMsg();
        }

        private bool GetWireTemSafeValueMsg()
        {
            bool bRet = false;
            MSG_WIRE_SAFEVALUE sDeviceGetInfo = new MSG_WIRE_SAFEVALUE();

            sDeviceGetInfo.nWireTemSafeValueLow = 0;
            sDeviceGetInfo.nWireTemSafeValueHigh = 0;

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取电线温度安全阈值
            bRet = SD_GetDeviceInfo(m_nDevHandle, 113, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if (!bRet)
            {
                MessageBox.Show("获取电线温度安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 113));
            m_sInstance.TabConsoleWrite("client->dev:获取电线温度安全阈值.\n");

            MSG_WIRE_SAFEVALUE sGetInfo = (MSG_WIRE_SAFEVALUE)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_WIRE_SAFEVALUE));

            float fValueL = 0.0f;
            float fValueH = 0.0f;
            string strValueL = "";
            string strValueH = "";

            fValueL = sGetInfo.nWireTemSafeValueLow * 0.1f - 40.0f;
            fValueH = sGetInfo.nWireTemSafeValueHigh * 0.1f - 40.0f;
            strValueL = string.Format("{0:D}℃", (int)fValueL);
            strValueH = string.Format("{0:D}℃", (int)fValueH);
            TabAccess_TextBox_Wire_SafeValue_L.Text = strValueL;
            TabAccess_TextBox_Wire_SafeValue_H.Text = strValueH;

            return true;
        }

        private void GetWireTemSafeValueMsgAll()
        {
            GetWireTemSafeValueMsg();
        }

        private void GetWireTemFreqMsgAll()
        {
            bool bRet = false;
            MSG_WIRE_FREQ sDeviceGetInfo = new MSG_WIRE_FREQ();

            sDeviceGetInfo.nWireTemFreq = 0;

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取电线温度推送频率
            bRet = SD_GetDeviceInfo(m_nDevHandle, 111, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if (!bRet)
            {
                MessageBox.Show("获取电线温度推送频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 111));
            m_sInstance.TabConsoleWrite("client->dev:获取电线温度推送频率.\n");

            MSG_WIRE_FREQ sGetInfo = (MSG_WIRE_FREQ)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_WIRE_FREQ));

            // 电线温度推送频率
            int nVar = 0;
            string strValue = "";

            nVar = sGetInfo.nWireTemFreq;
            strValue = string.Format("{0}s", nVar);
            TabAccess_TextBox_Wire_Freq.Text = strValue;
        }

        private void GetOutputSwitchMsgAll()
        {
            bool bRet = false;
            MSG_OUTPUT_STATUS sDeviceGetInfo = new MSG_OUTPUT_STATUS();

            sDeviceGetInfo.nStatus = new byte[4];
            Array.Clear(sDeviceGetInfo.nStatus, 0, sDeviceGetInfo.nStatus.Length);

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取IO输出开关状态
            bRet = SD_GetDeviceInfo(m_nDevHandle, 93, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if (!bRet)
            {
                MessageBox.Show("获取IO输出开关状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 93));
            m_sInstance.TabConsoleWrite("client->dev:获取IO输出开关状态.\n");

            MSG_OUTPUT_STATUS sGetInfo = (MSG_OUTPUT_STATUS)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_OUTPUT_STATUS));

            // IO输出开关状态
            if (sGetInfo.nStatus[0] == 0)
            {
                string strStatus = "";

                strStatus = "关闭";
                TabAccess_TextBox_Output1_Switch.BackColor = Color.Orange;
                TabAccess_TextBox_Output1_Switch.Text = strStatus;
            }
            else
            {
                string strStatus = "";

                strStatus = "开启";
                TabAccess_TextBox_Output1_Switch.BackColor = Color.Azure;
                TabAccess_TextBox_Output1_Switch.Text = strStatus;
            }

            if (sGetInfo.nStatus[1] == 0)
            {
                string strStatus = "";

                strStatus = "关闭";
                TabAccess_TextBox_Output2_Switch.BackColor = Color.Orange;
                TabAccess_TextBox_Output2_Switch.Text = strStatus;
            }
            else
            {
                string strStatus = "";

                strStatus = "开启";
                TabAccess_TextBox_Output2_Switch.BackColor = Color.Azure;
                TabAccess_TextBox_Output2_Switch.Text = strStatus;
            }

            if (sGetInfo.nStatus[2] == 0)
            {
                string strStatus = "";

                strStatus = "关闭";
                TabAccess_TextBox_Output3_Switch.BackColor = Color.Orange;
                TabAccess_TextBox_Output3_Switch.Text = strStatus;
            }
            else
            {
                string strStatus = "";

                strStatus = "开启";
                TabAccess_TextBox_Output3_Switch.BackColor = Color.Azure;
                TabAccess_TextBox_Output3_Switch.Text = strStatus;
            }

            if (sGetInfo.nStatus[3] == 0)
            {
                string strStatus = "";

                strStatus = "关闭";
                TabAccess_TextBox_Output4_Switch.BackColor = Color.Orange;
                TabAccess_TextBox_Output4_Switch.Text = strStatus;
            }
            else
            {
                string strStatus = "";

                strStatus = "开启";
                TabAccess_TextBox_Output4_Switch.BackColor = Color.Azure;
                TabAccess_TextBox_Output4_Switch.Text = strStatus;
            }

        }
        #endregion

        #region 设备控制相关函数

        private void SetUSBEnableMsgAll()
        {
            bool bRet = false;
            MSG_USB_ENABLE sDeviceSetInfo = new MSG_USB_ENABLE();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nStatus = new byte[24];
            Array.Clear(sDeviceSetInfo.nStatus, 0, sDeviceSetInfo.nStatus.Length);

            for(int i = 0; i < 24; ++i)
            {
                sDeviceSetInfo.nStatus[i] = 1;
            }

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置USB使能开关
            bRet = SD_SetDeviceInfo(m_nDevHandle, 102, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置USB使能开关失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 102));
            m_sInstance.TabConsoleWrite("client->dev:设置USB使能开关.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if(sGetInfo.nStatus == 0)
            {
                MessageBox.Show("设置USB使能开关成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }

        }

        private void SetInputEnableMsgAll()
        {
            bool bRet = false;
            MSG_INPUT_ENABLE sDeviceSetInfo = new MSG_INPUT_ENABLE();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nStatus = new byte[4];
            Array.Clear(sDeviceSetInfo.nStatus, 0, sDeviceSetInfo.nStatus.Length);

            for (int i = 0; i < 4; ++i)
            {
                sDeviceSetInfo.nStatus[i] = 1;
            }

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置IO输入使能开关
            bRet = SD_SetDeviceInfo(m_nDevHandle, 82, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置IO输入使能开关失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 82));
            m_sInstance.TabConsoleWrite("client->dev:设置IO输入使能开关.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus == 0)
            {
                MessageBox.Show("设置IO输入使能开关成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void SetTemAndHumEnableMsgAll()
        {
            bool bRet = false;
            MSG_TEMANDHUM_ENABLE sDeviceSetInfo = new MSG_TEMANDHUM_ENABLE();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nStatus1 = 0;
            sDeviceSetInfo.nStatus2 = 0;

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置温湿度使能开关
            bRet = SD_SetDeviceInfo(m_nDevHandle, 72, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置温湿度使能开关失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 72));
            m_sInstance.TabConsoleWrite("client->dev:设置温湿度使能开关.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus == 0)
            {
                MessageBox.Show("设置温湿度使能开关成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void SetWaterLeachEnableMsgAll()
        {
            bool bRet = false;
            MSG_WATERLEACH_ENABLE sDeviceSetInfo = new MSG_WATERLEACH_ENABLE();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nStatus = 0;

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置水浸使能开关
            bRet = SD_SetDeviceInfo(m_nDevHandle, 62, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置水浸使能开关失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 62));
            m_sInstance.TabConsoleWrite("client->dev:设置水浸使能开关.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus == 0)
            {
                MessageBox.Show("设置水浸使能开关成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private bool SetTemAndHumSafeValueMsg(MSG_TEMANDHUM_SAFEVALUE sSetInfo)
        {
            bool bRet = false;
            MSG_TEMANDHUM_SAFEVALUE sDeviceSetInfo = new MSG_TEMANDHUM_SAFEVALUE();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nPort = sSetInfo.nPort;
            sDeviceSetInfo.nID = sSetInfo.nID;
            sDeviceSetInfo.nTemAndHumSafeValueLow = sSetInfo.nTemAndHumSafeValueLow;
            sDeviceSetInfo.nTemAndHumSafeValueHigh = sSetInfo.nTemAndHumSafeValueHigh;

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置温湿度安全阈值
            bRet = SD_SetDeviceInfo(m_nDevHandle, 76, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置温湿度安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 76));
            m_sInstance.TabConsoleWrite("client->dev:设置温湿度安全阈值.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置温湿度安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void SetTemAndHumSafeValueMsgAll()
        {
            MSG_TEMANDHUM_SAFEVALUE sSetInfo = new MSG_TEMANDHUM_SAFEVALUE();

            // 温度1
            sSetInfo.nPort = 1;
            sSetInfo.nID = 1;
            sSetInfo.nTemAndHumSafeValueLow = (Int16)(Convert.ToInt16(TabControl_TextBox_Tem1_SafeValueL.Text) * 10 + 400);
            sSetInfo.nTemAndHumSafeValueHigh = (Int16)(Convert.ToInt16(TabControl_TextBox_Tem1_SafeValueH.Text) * 10 + 400);

            SetTemAndHumSafeValueMsg(sSetInfo);

            // 湿度1
            sSetInfo.nPort = 1;
            sSetInfo.nID = 2;
            sSetInfo.nTemAndHumSafeValueLow = (Int16)(Convert.ToInt16(TabControl_TextBox_Hum1_SafeValueL.Text) * 10);
            sSetInfo.nTemAndHumSafeValueHigh = (Int16)(Convert.ToInt16(TabControl_TextBox_Hum1_SafeValueH.Text) * 10);

            SetTemAndHumSafeValueMsg(sSetInfo);

            // 温度2
            sSetInfo.nPort = 2;
            sSetInfo.nID = 1;
            sSetInfo.nTemAndHumSafeValueLow = (Int16)(Convert.ToInt16(TabControl_TextBox_Tem2_SafeValueL.Text) * 10 + 400);
            sSetInfo.nTemAndHumSafeValueHigh = (Int16)(Convert.ToInt16(TabControl_TextBox_Tem2_SafeValueH.Text) * 10 + 400);

            SetTemAndHumSafeValueMsg(sSetInfo);

            // 湿度2
            sSetInfo.nPort = 2;
            sSetInfo.nID = 2;
            sSetInfo.nTemAndHumSafeValueLow = (Int16)(Convert.ToInt16(TabControl_TextBox_Hum2_SafeValueL.Text) * 10);
            sSetInfo.nTemAndHumSafeValueHigh = (Int16)(Convert.ToInt16(TabControl_TextBox_Hum2_SafeValueH.Text) * 10);

            SetTemAndHumSafeValueMsg(sSetInfo);

        }

        private void SetTemAndHumFreqMsgAll()
        {
            bool bRet = false;
            MSG_TEMANDHUM_FREQ sDeviceSetInfo = new MSG_TEMANDHUM_FREQ();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nTemAndHumFreq = (Int16)(Convert.ToInt16(TabControl_TextBox_Tem1_Freq.Text));

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置温湿度监测频率
            bRet = SD_SetDeviceInfo(m_nDevHandle, 74, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置温湿度监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 74));
            m_sInstance.TabConsoleWrite("client->dev:设置温湿度监测频率.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置温湿度监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        private void SetWireTemSafeValueMsgAll()
        {
            bool bRet = false;
            MSG_WIRE_SAFEVALUE sDeviceSetInfo = new MSG_WIRE_SAFEVALUE();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nWireTemSafeValueLow = (Int16)(Convert.ToInt16(TabControl_TextBox_Wire_SafeValueL.Text) * 10 + 400);
            sDeviceSetInfo.nWireTemSafeValueHigh = (Int16)(Convert.ToInt16(TabControl_TextBox_Wire_SafeValueH.Text) * 10 + 400);

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置电线温度安全阈值
            bRet = SD_SetDeviceInfo(m_nDevHandle, 114, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置电线温度安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 114));
            m_sInstance.TabConsoleWrite("client->dev:设置电线温度安全阈值.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置电线温度安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        private void SetWireTemFreqMsgAll()
        {
            bool bRet = false;
            MSG_WIRE_FREQ sDeviceSetInfo = new MSG_WIRE_FREQ();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nWireTemFreq = (Int16)(Convert.ToInt16(TabControl_TextBox_Wire_Freq.Text));

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置电线温度监测频率
            bRet = SD_SetDeviceInfo(m_nDevHandle, 112, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置电线温度监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 112));
            m_sInstance.TabConsoleWrite("client->dev:设置电线温度监测频率.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置电线温度监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        private void SetOutputSwitchMsgAll()
        {
            bool bRet = false;
            MSG_OUTPUT_STATUS sDeviceSetInfo = new MSG_OUTPUT_STATUS();
            MSG_OUTPUT_STATUS sSetInfoOn = new MSG_OUTPUT_STATUS();
            MSG_OUTPUT_STATUS sSetInfoOff = new MSG_OUTPUT_STATUS();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nStatus = new byte[4];
            Array.Clear(sDeviceSetInfo.nStatus, 0, sDeviceSetInfo.nStatus.Length);
            sSetInfoOn.nStatus = new byte[4];
            Array.Clear(sSetInfoOn.nStatus, 0, sSetInfoOn.nStatus.Length);
            sSetInfoOff.nStatus = new byte[4];
            Array.Clear(sSetInfoOff.nStatus, 0, sSetInfoOff.nStatus.Length);

            if (TabControl_Radio_Output1_On.Checked == true)
            {
                sDeviceSetInfo.nStatus[3] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[3] = 0;
            }

            if (TabControl_Radio_Output2_On.Checked == true)
            {
                sDeviceSetInfo.nStatus[2] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[2] = 0;
            }

            if (TabControl_Radio_Output3_On.Checked == true)
            {
                sDeviceSetInfo.nStatus[1] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[1] = 0;
            }

            if (TabControl_Radio_Output4_On.Checked == true)
            {
                sDeviceSetInfo.nStatus[0] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[0] = 0;
            }

            for(int i = 0; i < 4; ++i)
            {
                sSetInfoOn.nStatus[i] = sDeviceSetInfo.nStatus[i];
                sSetInfoOff.nStatus[i] = (byte)((~sDeviceSetInfo.nStatus[i]) & 0x01);
            }

            int nT1Size = Marshal.SizeOf(sSetInfoOn);
            IntPtr pSetInfoOn = Marshal.AllocHGlobal(nT1Size);
            Marshal.StructureToPtr(sSetInfoOn, pSetInfoOn, true);

            int nT2Size = Marshal.SizeOf(sSetInfoOff);
            IntPtr pSetInfoOff = Marshal.AllocHGlobal(nT2Size);
            Marshal.StructureToPtr(sSetInfoOff, pSetInfoOff, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置IO输出开
            bRet = SD_SetDeviceInfo(m_nDevHandle, 91, pSetInfoOn, nT1Size, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置IO输出开失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 91));
            m_sInstance.TabConsoleWrite("client->dev:设置IO输出开.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置IO输出开失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 设置IO输出关
            bRet = SD_SetDeviceInfo(m_nDevHandle, 92, pSetInfoOff, nT1Size, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置IO输出关失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 92));
            m_sInstance.TabConsoleWrite("client->dev:设置IO输出关.\n");

            sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置IO输出关失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        private void SetInputTurnMsgAll()
        {
            bool bRet = false;
            MSG_INPUT_TURN sDeviceSetInfo = new MSG_INPUT_TURN();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nStatus = new byte[4];
            Array.Clear(sDeviceSetInfo.nStatus, 0, sDeviceSetInfo.nStatus.Length);

            if (TabControl_Radio_Input1_H.Checked == true)
            {
                sDeviceSetInfo.nStatus[0] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[0] = 0;
            }

            if (TabControl_Radio_Input2_H.Checked == true)
            {
                sDeviceSetInfo.nStatus[1] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[1] = 0;
            }

            if (TabControl_Radio_Input3_H.Checked == true)
            {
                sDeviceSetInfo.nStatus[2] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[2] = 0;
            }

            if (TabControl_Radio_Input4_H.Checked == true)
            {
                sDeviceSetInfo.nStatus[3] = 1;
            }
            else
            {
                sDeviceSetInfo.nStatus[3] = 0;
            }

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置IO输入电平反转
            bRet = SD_SetDeviceInfo(m_nDevHandle, 83, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置IO输入电平反转失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 83));
            m_sInstance.TabConsoleWrite("client->dev:设置IO输入电平反转.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置IO输入电平反转失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        private void SetElecFreqMsgAll()
        {
            bool bRet = false;
            MSG_ELECTRIC_FREQ sDeviceSetInfo = new MSG_ELECTRIC_FREQ();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nElectricFreq = (Int16)(Convert.ToInt16(TabControl_TextBox_Elec_Freq.Text));

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置市电状态监测频率
            bRet = SD_SetDeviceInfo(m_nDevHandle, 32, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置市电状态监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 32));
            m_sInstance.TabConsoleWrite("client->dev:设置市电状态监测频率.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置市电状态监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        private void SetDevFreqMsgAll()
        {
            bool bRet = false;
            MSG_DEVICE_FREQ sDeviceSetInfo = new MSG_DEVICE_FREQ();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nDeviceFreq = (Int16)(Convert.ToInt16(TabControl_TextBox_Dev_Freq.Text));

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置设备状态监测频率
            bRet = SD_SetDeviceInfo(m_nDevHandle, 25, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置设备状态监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 25));
            m_sInstance.TabConsoleWrite("client->dev:设置设备状态监测频率.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置设备状态监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        private bool SetPowerOutputSafeValueMsg(MSG_POWEROUT_SAFEVALUE sSetInfo)
        {
            bool bRet = false;
            MSG_POWEROUT_SAFEVALUE sDeviceSetInfo = new MSG_POWEROUT_SAFEVALUE();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nPort = sSetInfo.nPort;
            sDeviceSetInfo.nCurrentSafeLow = sSetInfo.nCurrentSafeLow;
            sDeviceSetInfo.nCurrentSafeHigh = sSetInfo.nCurrentSafeHigh;
            sDeviceSetInfo.nVoltageSafeLow = sSetInfo.nVoltageSafeLow;
            sDeviceSetInfo.nVoltageSafeHigh = sSetInfo.nVoltageSafeHigh;

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置电源输出安全阈值
            bRet = SD_SetDeviceInfo(m_nDevHandle, 44, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置电源输出安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 44));
            m_sInstance.TabConsoleWrite("client->dev:设置电源输出安全阈值.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置电源输出安全阈值失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void SetPowerOutputSafeValueMsgAll()
        {
            MSG_POWEROUT_SAFEVALUE sSetInfo = new MSG_POWEROUT_SAFEVALUE();

            // 电源1
            sSetInfo.nPort = 1;
            sSetInfo.nCurrentSafeLow = 0;
            sSetInfo.nCurrentSafeHigh = (byte)(Convert.ToDouble(TabControl_TextBox_Power1_SafeValueA.Text) * 10);
            sSetInfo.nVoltageSafeLow = Convert.ToByte(TabControl_TextBox_Power1_SafeValueVL.Text);
            sSetInfo.nVoltageSafeHigh = Convert.ToByte(TabControl_TextBox_Power1_SafeValueVH.Text);

            SetPowerOutputSafeValueMsg(sSetInfo);

            // 电源2
            sSetInfo.nPort = 2;
            sSetInfo.nCurrentSafeLow = 0;
            sSetInfo.nCurrentSafeHigh = (byte)(Convert.ToDouble(TabControl_TextBox_Power2_SafeValueA.Text) * 10);
            sSetInfo.nVoltageSafeLow = Convert.ToByte(TabControl_TextBox_Power2_SafeValueVL.Text);
            sSetInfo.nVoltageSafeHigh = Convert.ToByte(TabControl_TextBox_Power2_SafeValueVH.Text);

            SetPowerOutputSafeValueMsg(sSetInfo);

            // 电源3
            sSetInfo.nPort = 3;
            sSetInfo.nCurrentSafeLow = 0;
            sSetInfo.nCurrentSafeHigh = (byte)(Convert.ToDouble(TabControl_TextBox_Power3_SafeValueA.Text) * 10);
            sSetInfo.nVoltageSafeLow = Convert.ToByte(TabControl_TextBox_Power3_SafeValueVL.Text);
            sSetInfo.nVoltageSafeHigh = Convert.ToByte(TabControl_TextBox_Power3_SafeValueVH.Text);

            SetPowerOutputSafeValueMsg(sSetInfo);

        }

        private bool SetPowerOutputFreqMsg(MSG_POWEROUT_FREQ sSetInfo)
        {
            bool bRet = false;
            MSG_POWEROUT_FREQ sDeviceSetInfo = new MSG_POWEROUT_FREQ();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nPort = sSetInfo.nPort;
            sDeviceSetInfo.nPowerOutputFreq = sSetInfo.nPowerOutputFreq;

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置电源输出监测频率
            bRet = SD_SetDeviceInfo(m_nDevHandle, 42, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置电源输出监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 42));
            m_sInstance.TabConsoleWrite("client->dev:设置电源输出监测频率.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置电源输出监测频率失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void SetPowerOutputFreqMsgAll()
        {
            MSG_POWEROUT_FREQ sSetInfo = new MSG_POWEROUT_FREQ();

            // 电源1
            sSetInfo.nPort = 1;
            sSetInfo.nPowerOutputFreq = (Int16)(Convert.ToInt16(TabControl_TextBox_Power1_Freq.Text));

            SetPowerOutputFreqMsg(sSetInfo);

            // 电源2
            sSetInfo.nPort = 2;
            sSetInfo.nPowerOutputFreq = (Int16)(Convert.ToInt16(TabControl_TextBox_Power2_Freq.Text));

            SetPowerOutputFreqMsg(sSetInfo);

            // 电源3
            sSetInfo.nPort = 3;
            sSetInfo.nPowerOutputFreq = (Int16)(Convert.ToInt16(TabControl_TextBox_Power3_Freq.Text));

            SetPowerOutputFreqMsg(sSetInfo);

        }

        private bool SetPowerOutputSwitchMsg(MSG_POWEROUT_STATUS sSetInfo)
        {
            bool bRet = false;
            MSG_POWEROUT_STATUS sDeviceSetInfo = new MSG_POWEROUT_STATUS();
            MSG_CALLBACK sCallbackInfo = new MSG_CALLBACK();

            sDeviceSetInfo.nPort = sSetInfo.nPort;
            sDeviceSetInfo.nStatus = sSetInfo.nStatus;

            int nTSize = Marshal.SizeOf(sDeviceSetInfo);
            IntPtr pDeviceSetInfo = Marshal.AllocHGlobal(nTSize);
            Marshal.StructureToPtr(sDeviceSetInfo, pDeviceSetInfo, true);

            sCallbackInfo.nStatus = 0;

            int nRSize = Marshal.SizeOf(sCallbackInfo);
            IntPtr pCallbackInfo = Marshal.AllocHGlobal(nRSize);
            Marshal.StructureToPtr(sCallbackInfo, pCallbackInfo, true);

            // 设置电源输出开关状态
            bRet = SD_SetDeviceInfo(m_nDevHandle, 48, pDeviceSetInfo, nTSize, pCallbackInfo, nRSize);
            if (!bRet)
            {
                MessageBox.Show("设置电源输出开关状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 48));
            m_sInstance.TabConsoleWrite("client->dev:设置电源输出开关状态.\n");

            MSG_CALLBACK sGetInfo = (MSG_CALLBACK)Marshal.PtrToStructure(pCallbackInfo, typeof(MSG_CALLBACK));

            if (sGetInfo.nStatus != 0)
            {
                MessageBox.Show("设置电源输出开关状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;

        }

        private void SetPowerOutputSwitchMsgAll()
        {
            MSG_POWEROUT_STATUS sSetInfo = new MSG_POWEROUT_STATUS();

            // 电源1
            sSetInfo.nPort = 1;
            if(TabControl_Radio_Power1_On.Checked == true)
            {
                sSetInfo.nStatus = 1;
            }
            else
            {
                sSetInfo.nStatus = 0;
            }

            SetPowerOutputSwitchMsg(sSetInfo);

            // 电源2
            sSetInfo.nPort = 2;
            if (TabControl_Radio_Power2_On.Checked == true)
            {
                sSetInfo.nStatus = 1;
            }
            else
            {
                sSetInfo.nStatus = 0;
            }

            SetPowerOutputSwitchMsg(sSetInfo);

            // 电源3
            sSetInfo.nPort = 3;
            if (TabControl_Radio_Power3_On.Checked == true)
            {
                sSetInfo.nStatus = 1;
            }
            else
            {
                sSetInfo.nStatus = 0;
            }

            SetPowerOutputSwitchMsg(sSetInfo);

        }

        #endregion

        #region 消息回调函数
        public static void FormMain_DeviceMessageCallback(Int32 lDevHandle, Int32 nMessageType, IntPtr pMessage, Int32 nMessageSize, IntPtr pUser)
        {
            switch (nMessageType)
            {
                case 1:
                    // 设备离线处理
                    m_sInstance.HandleDeviceOfflineProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 22:
                    // 主动推送设备状态处理
                    m_sInstance.HandlePushDeviceStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 23:
                    // 心跳推送设备状态处理
                    m_sInstance.HandleHeartDeviceStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 34:
                    // 主动推送市电当前状态处理
                    m_sInstance.HandlePushElectricStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 35:
                    // 推送市电断电报警处理
                    m_sInstance.HandleAlarmElectricStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 46:
                    // 电源输出主动推送值处理
                    m_sInstance.HandlePushPowerOutputStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 47:
                    // 电源输出过载报警处理
                    m_sInstance.HandleAlarmPowerOutputStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 63:
                    // 水浸状态报警处理
                    m_sInstance.HandleAlarmWaterLeachStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 78:
                    // 温湿度主动推送值处理
                    m_sInstance.HandlePushTemAndHumStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 79:
                    // 温湿度过载报警处理
                    m_sInstance.HandleAlarmTemAndHumStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 85:
                    // 主动推送IO输入当前状态处理
                    m_sInstance.HandlePushInputStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 86:
                    // IO输入异常报警处理
                    m_sInstance.HandleAlarmInputStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 104:
                    // USB异常状态推送处理
                    m_sInstance.HandleAlarmUSBStatusProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 116:
                    // 电线温度主动推送值处理
                    m_sInstance.HandlePushWireTemValueProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                case 117:
                    // 电线温度过载报警处理
                    m_sInstance.HandleAlarmWireTemValueProcess(lDevHandle, nMessageType, pMessage, nMessageSize, pUser);
                    break;
                default:
                    m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", lDevHandle, nMessageType));
                    m_sInstance.TabConsoleWrite("client<-dev:未知消息.\n");
                    break;
            }

        }
        #endregion

        #region 登录标签页消息响应
        private void TabLogin_Button_Login_Click(object sender, EventArgs e)    // 设备连接
        {
            string sDevIp = string.Empty;
            string sDevPort = string.Empty;
            string sHostIp = string.Empty;
            string sHostPort = string.Empty;

            sDevIp = TabLogin_TextBox_DeviceIP.Text;        // 设备IP地址
            sDevPort = TabLogin_TextBox_DevicePort.Text;    // 设备端口号
            sHostIp = TabLogin_TextBox_HostIP.Text;         // 本地IP地址
            sHostPort = TabLogin_TextBox_HostPort.Text;     // 本地端口号

            int nDevPort = 0;
            int nHostPort = 0;

            // 检测设备IP地址
            if(!IsIPAddress(sDevIp))
            {
                MessageBox.Show("设备IP地址不合法!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 检测本机IP地址
            if (!IsIPAddress(sHostIp))
            {
                MessageBox.Show("本机IP地址不合法!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            nDevPort = Convert.ToInt16(sDevPort);
            nHostPort = Convert.ToInt16(sHostPort);

            // 检测设备端口号
            if(nDevPort < 0 || nDevPort > 65535)
            {
                MessageBox.Show("设备端口号不合法!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 检测本机端口号
            if (nHostPort < 0 || nHostPort > 65535)
            {
                MessageBox.Show("本机端口号不合法!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 设备登录相关
            SetIpAndPort(sHostIp, nHostPort);
            m_nDevHandle = LoginDevice(sDevIp, nDevPort);
            if(-1 != m_nDevHandle)
            {
                TabLogin_Label_DeviceIPState.Text = "设备IP地址:" + sDevIp;
                TabLogin_Label_DevicePortState.Text = "设备端口号:" + sDevPort;
                TabLogin_Label_HostIPStatus.Text = "本机IP地址:" + sHostIp;
                TabLogin_Label_HostPortStatus.Text = "本机端口号:" + sHostPort;
                TabLogin_Label_Status.Text = "状态:在线";
            }

        }

        private void TabLogin_Button_Logout_Click(object sender, EventArgs e)   // 设备断开
        {
            if(-1 != m_nDevHandle)
            {
                DeleteDeviceAll();
                m_nDevHandle = -1;
                TabLogin_Label_DeviceIPState.Text = "设备IP地址:";
                TabLogin_Label_DevicePortState.Text = "设备端口号:";
                TabLogin_Label_HostIPStatus.Text = "本机IP地址:";
                TabLogin_Label_HostPortStatus.Text = "本机端口号:";
                TabLogin_Label_Status.Text = "状态:离线";
            }
        }

        private void TabLogin_TextBox_DeviceIP_KeyPress(object sender, KeyPressEventArgs e) // 设备IP地址输入
        {

        }

        private void TabLogin_TextBox_DevicePort_KeyPress(object sender, KeyPressEventArgs e)   // 设备端口号输入
        {
            if (e.KeyChar == 0x20) e.KeyChar = (char)0;  //禁止空格键
            if ((e.KeyChar == 0x2D) && (((TextBox)sender).Text.Length == 0)) return;   //处理负数
            if (e.KeyChar > 0x20)
            {
                try
                {
                    double.Parse(((TextBox)sender).Text + e.KeyChar.ToString());
                }
                catch
                {
                    e.KeyChar = (char)0;   //处理非法字符
                }
            }
        }

        private void TabLogin_TextBox_HostIP_KeyPress(object sender, KeyPressEventArgs e)   // 本机IP地址输入
        {

        }

        private void TabLogin_TextBox_HostPort_KeyPress(object sender, KeyPressEventArgs e) // 本机端口号输入
        {
            if (e.KeyChar == 0x20) e.KeyChar = (char)0;  //禁止空格键
            if ((e.KeyChar == 0x2D) && (((TextBox)sender).Text.Length == 0)) return;   //处理负数
            if (e.KeyChar > 0x20)
            {
                try
                {
                    double.Parse(((TextBox)sender).Text + e.KeyChar.ToString());
                }
                catch
                {
                    e.KeyChar = (char)0;   //处理非法字符
                }
            }
        }

        #endregion

        #region 控制台标签页消息响应
        private void TabConsole_Button_Clear_Click(object sender, EventArgs e)  // 清空控制台
        {
            TabConsole_TextBox_Console.Clear();
        }
        #endregion

        #region 报警标签页消息响应
        private void TabAlarm_Button_GetAllStatus_Click(object sender, EventArgs e) // 获取设备状态
        {
            bool bRet = false;
            MSG_DEVICE_STATUS sDeviceGetInfo = new MSG_DEVICE_STATUS();

            sDeviceGetInfo.nUSBStatus = new byte[24];
            sDeviceGetInfo.nInputStatus = new byte[4];
            sDeviceGetInfo.nOutputStatus = new byte[4];
            sDeviceGetInfo.nPowerOutputAStatus = new byte[3];
            sDeviceGetInfo.nPowerOutputVStatus = new byte[3];
            sDeviceGetInfo.nTemStatus = new byte[2];
            sDeviceGetInfo.nHumStatus = new byte[2];
            sDeviceGetInfo.nElectricStatus = 0;
            sDeviceGetInfo.nPowerInputStatus = 0;
            sDeviceGetInfo.nPower12VStatus = 0;
            sDeviceGetInfo.nWaterLeachStatus = 0;
            sDeviceGetInfo.nWireTemStatus = 0;
            Array.Clear(sDeviceGetInfo.nUSBStatus, 0, sDeviceGetInfo.nUSBStatus.Length);
            Array.Clear(sDeviceGetInfo.nInputStatus, 0, sDeviceGetInfo.nInputStatus.Length);
            Array.Clear(sDeviceGetInfo.nOutputStatus, 0, sDeviceGetInfo.nOutputStatus.Length);
            Array.Clear(sDeviceGetInfo.nPowerOutputAStatus, 0, sDeviceGetInfo.nPowerOutputAStatus.Length);
            Array.Clear(sDeviceGetInfo.nPowerOutputVStatus, 0, sDeviceGetInfo.nPowerOutputVStatus.Length);
            Array.Clear(sDeviceGetInfo.nTemStatus, 0, sDeviceGetInfo.nTemStatus.Length);
            Array.Clear(sDeviceGetInfo.nHumStatus, 0, sDeviceGetInfo.nHumStatus.Length);

            int nSize = Marshal.SizeOf(sDeviceGetInfo);
            IntPtr pDeviceGetInfo = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(sDeviceGetInfo, pDeviceGetInfo, true);

            // 获取设备状态
            bRet = SD_GetDeviceInfo(m_nDevHandle, 21, IntPtr.Zero, 0, pDeviceGetInfo, nSize);
            if(!bRet)
            {
                MessageBox.Show("获取设备状态失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_sInstance.TabConsoleWrite(string.Format("句柄:{0}, 消息:{1}.\n", m_nDevHandle, 21));
            m_sInstance.TabConsoleWrite("client->dev:获取设备状态.\n");

            MSG_DEVICE_STATUS sGetInfo = (MSG_DEVICE_STATUS)Marshal.PtrToStructure(pDeviceGetInfo, typeof(MSG_DEVICE_STATUS));

            // 温度状态
            if(sGetInfo.nTemStatus[0] == 0)
            {
                TabAlarm_TextBox_Tem1.Text = "正常";
                TabAlarm_TextBox_Tem1.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Tem1.Text = "报警";
                TabAlarm_TextBox_Tem1.BackColor = Color.LightCoral;
            }
            if (sGetInfo.nTemStatus[1] == 0)

            {
                TabAlarm_TextBox_Tem2.Text = "正常";
                TabAlarm_TextBox_Tem2.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Tem2.Text = "报警";
                TabAlarm_TextBox_Tem2.BackColor = Color.LightCoral;
            }

            // 湿度状态
            if(sGetInfo.nHumStatus[0] == 0)
            {
                TabAlarm_TextBox_Hum1.Text = "正常";
                TabAlarm_TextBox_Hum1.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Hum1.Text = "报警";
                TabAlarm_TextBox_Hum1.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nHumStatus[1] == 0)
            {
                TabAlarm_TextBox_Hum2.Text = "正常";
                TabAlarm_TextBox_Hum2.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Hum2.Text = "报警";
                TabAlarm_TextBox_Hum2.BackColor = Color.LightCoral;
            }

            // 电线温度状态
            if (sGetInfo.nWireTemStatus == 0)
            {
                TabAlarm_TextBox_Wire.Text = "正常";
                TabAlarm_TextBox_Wire.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Wire.Text = "报警";
                TabAlarm_TextBox_Wire.BackColor = Color.LightCoral;
            }

            // 水浸状态
            if(sGetInfo.nWaterLeachStatus == 0)
            {
                TabAlarm_TextBox_Water.Text = "正常";
                TabAlarm_TextBox_Water.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Water.Text = "报警";
                TabAlarm_TextBox_Water.BackColor = Color.LightCoral;
            }

            // 市电状态
            if(sGetInfo.nElectricStatus == 0)
            {
                TabAlarm_TextBox_Electric.Text = "正常";
                TabAlarm_TextBox_Electric.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Electric.Text = "报警";
                TabAlarm_TextBox_Electric.BackColor = Color.LightCoral;
            }

            // 电源输出状态
            if(sGetInfo.nPowerOutputAStatus[0] == 0)
            {
                TabAlarm_TextBox_PowerOutput1A.Text = "正常";
                TabAlarm_TextBox_PowerOutput1A.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_PowerOutput1A.Text = "报警";
                TabAlarm_TextBox_PowerOutput1A.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nPowerOutputAStatus[1] == 0)
            {
                TabAlarm_TextBox_PowerOutput2A.Text = "正常";
                TabAlarm_TextBox_PowerOutput2A.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_PowerOutput2A.Text = "报警";
                TabAlarm_TextBox_PowerOutput2A.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nPowerOutputAStatus[2] == 0)
            {
                TabAlarm_TextBox_PowerOutput3A.Text = "正常";
                TabAlarm_TextBox_PowerOutput3A.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_PowerOutput3A.Text = "报警";
                TabAlarm_TextBox_PowerOutput3A.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nPowerOutputVStatus[0] == 0)
            {
                TabAlarm_TextBox_PowerOutput1V.Text = "正常";
                TabAlarm_TextBox_PowerOutput1V.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_PowerOutput1V.Text = "报警";
                TabAlarm_TextBox_PowerOutput1V.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nPowerOutputVStatus[1] == 0)
            {
                TabAlarm_TextBox_PowerOutput2V.Text = "正常";
                TabAlarm_TextBox_PowerOutput2V.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_PowerOutput2V.Text = "报警";
                TabAlarm_TextBox_PowerOutput2V.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nPowerOutputVStatus[2] == 0)
            {
                TabAlarm_TextBox_PowerOutput3V.Text = "正常";
                TabAlarm_TextBox_PowerOutput3V.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_PowerOutput3V.Text = "报警";
                TabAlarm_TextBox_PowerOutput3V.BackColor = Color.LightCoral;
            }

            // IO输入状态
            if(sGetInfo.nInputStatus[0] == 0)
            {
                TabAlarm_TextBox_Input1.Text = "正常";
                TabAlarm_TextBox_Input1.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Input1.Text = "报警";
                TabAlarm_TextBox_Input1.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nInputStatus[1] == 0)
            {
                TabAlarm_TextBox_Input2.Text = "正常";
                TabAlarm_TextBox_Input2.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Input2.Text = "报警";
                TabAlarm_TextBox_Input2.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nInputStatus[2] == 0)
            {
                TabAlarm_TextBox_Input3.Text = "正常";
                TabAlarm_TextBox_Input3.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Input3.Text = "报警";
                TabAlarm_TextBox_Input3.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nInputStatus[3] == 0)
            {
                TabAlarm_TextBox_Input4.Text = "正常";
                TabAlarm_TextBox_Input4.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_Input4.Text = "报警";
                TabAlarm_TextBox_Input4.BackColor = Color.LightCoral;
            }

            // IO输出状态
            if (sGetInfo.nOutputStatus[0] == 0)
            {
                TabAlarm_TextBox_Output1.Text = "关闭";
                TabAlarm_TextBox_Output1.BackColor = Color.Orange;
            }
            else
            {
                TabAlarm_TextBox_Output1.Text = "开启";
                TabAlarm_TextBox_Output1.BackColor = Color.LightBlue;
            }

            if (sGetInfo.nOutputStatus[1] == 0)
            {
                TabAlarm_TextBox_Output2.Text = "关闭";
                TabAlarm_TextBox_Output2.BackColor = Color.Orange;
            }
            else
            {
                TabAlarm_TextBox_Output2.Text = "开启";
                TabAlarm_TextBox_Output2.BackColor = Color.LightBlue;
            }

            if (sGetInfo.nOutputStatus[2] == 0)
            {
                TabAlarm_TextBox_Output3.Text = "关闭";
                TabAlarm_TextBox_Output3.BackColor = Color.Orange;
            }
            else
            {
                TabAlarm_TextBox_Output3.Text = "开启";
                TabAlarm_TextBox_Output3.BackColor = Color.LightBlue;
            }

            if (sGetInfo.nOutputStatus[3] == 0)
            {
                TabAlarm_TextBox_Output4.Text = "关闭";
                TabAlarm_TextBox_Output4.BackColor = Color.Orange;
            }
            else
            {
                TabAlarm_TextBox_Output4.Text = "开启";
                TabAlarm_TextBox_Output4.BackColor = Color.LightBlue;
            }

            // USB状态
            if (sGetInfo.nUSBStatus[0] == 0)
            {
                TabAlarm_TextBox_USB1.Text = "正常";
                TabAlarm_TextBox_USB1.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB1.Text = "报警";
                TabAlarm_TextBox_USB1.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[1] == 0)
            {
                TabAlarm_TextBox_USB2.Text = "正常";
                TabAlarm_TextBox_USB2.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB2.Text = "报警";
                TabAlarm_TextBox_USB2.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[2] == 0)
            {
                TabAlarm_TextBox_USB3.Text = "正常";
                TabAlarm_TextBox_USB3.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB3.Text = "报警";
                TabAlarm_TextBox_USB3.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[3] == 0)
            {
                TabAlarm_TextBox_USB4.Text = "正常";
                TabAlarm_TextBox_USB4.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB4.Text = "报警";
                TabAlarm_TextBox_USB4.BackColor = Color.LightCoral;
            }

            //========================================================
            if (sGetInfo.nUSBStatus[4] == 0)
            {
                TabAlarm_TextBox_USB5.Text = "正常";
                TabAlarm_TextBox_USB5.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB5.Text = "报警";
                TabAlarm_TextBox_USB5.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[5] == 0)
            {
                TabAlarm_TextBox_USB6.Text = "正常";
                TabAlarm_TextBox_USB6.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB6.Text = "报警";
                TabAlarm_TextBox_USB6.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[6] == 0)
            {
                TabAlarm_TextBox_USB7.Text = "正常";
                TabAlarm_TextBox_USB7.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB7.Text = "报警";
                TabAlarm_TextBox_USB7.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[7] == 0)
            {
                TabAlarm_TextBox_USB8.Text = "正常";
                TabAlarm_TextBox_USB8.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB8.Text = "报警";
                TabAlarm_TextBox_USB8.BackColor = Color.LightCoral;
            }

            //========================================================
            if (sGetInfo.nUSBStatus[8] == 0)
            {
                TabAlarm_TextBox_USB9.Text = "正常";
                TabAlarm_TextBox_USB9.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB9.Text = "报警";
                TabAlarm_TextBox_USB9.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[9] == 0)
            {
                TabAlarm_TextBox_USB10.Text = "正常";
                TabAlarm_TextBox_USB10.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB10.Text = "报警";
                TabAlarm_TextBox_USB10.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[10] == 0)
            {
                TabAlarm_TextBox_USB11.Text = "正常";
                TabAlarm_TextBox_USB11.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB11.Text = "报警";
                TabAlarm_TextBox_USB11.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[11] == 0)
            {
                TabAlarm_TextBox_USB12.Text = "正常";
                TabAlarm_TextBox_USB12.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB12.Text = "报警";
                TabAlarm_TextBox_USB12.BackColor = Color.LightCoral;
            }

            //========================================================
            if (sGetInfo.nUSBStatus[12] == 0)
            {
                TabAlarm_TextBox_USB13.Text = "正常";
                TabAlarm_TextBox_USB13.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB13.Text = "报警";
                TabAlarm_TextBox_USB13.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[13] == 0)
            {
                TabAlarm_TextBox_USB14.Text = "正常";
                TabAlarm_TextBox_USB14.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB14.Text = "报警";
                TabAlarm_TextBox_USB14.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[14] == 0)
            {
                TabAlarm_TextBox_USB15.Text = "正常";
                TabAlarm_TextBox_USB15.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB15.Text = "报警";
                TabAlarm_TextBox_USB15.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[15] == 0)
            {
                TabAlarm_TextBox_USB16.Text = "正常";
                TabAlarm_TextBox_USB16.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB16.Text = "报警";
                TabAlarm_TextBox_USB16.BackColor = Color.LightCoral;
            }

            //========================================================
            if (sGetInfo.nUSBStatus[16] == 0)
            {
                TabAlarm_TextBox_USB17.Text = "正常";
                TabAlarm_TextBox_USB17.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB17.Text = "报警";
                TabAlarm_TextBox_USB17.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[17] == 0)
            {
                TabAlarm_TextBox_USB18.Text = "正常";
                TabAlarm_TextBox_USB18.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB18.Text = "报警";
                TabAlarm_TextBox_USB18.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[18] == 0)
            {
                TabAlarm_TextBox_USB19.Text = "正常";
                TabAlarm_TextBox_USB19.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB19.Text = "报警";
                TabAlarm_TextBox_USB19.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[19] == 0)
            {
                TabAlarm_TextBox_USB20.Text = "正常";
                TabAlarm_TextBox_USB20.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB20.Text = "报警";
                TabAlarm_TextBox_USB20.BackColor = Color.LightCoral;
            }

            //========================================================
            if (sGetInfo.nUSBStatus[20] == 0)
            {
                TabAlarm_TextBox_USB21.Text = "正常";
                TabAlarm_TextBox_USB21.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB21.Text = "报警";
                TabAlarm_TextBox_USB21.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[21] == 0)
            {
                TabAlarm_TextBox_USB22.Text = "正常";
                TabAlarm_TextBox_USB22.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB22.Text = "报警";
                TabAlarm_TextBox_USB22.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[22] == 0)
            {
                TabAlarm_TextBox_USB23.Text = "正常";
                TabAlarm_TextBox_USB23.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB23.Text = "报警";
                TabAlarm_TextBox_USB23.BackColor = Color.LightCoral;
            }

            if (sGetInfo.nUSBStatus[23] == 0)
            {
                TabAlarm_TextBox_USB24.Text = "正常";
                TabAlarm_TextBox_USB24.BackColor = Color.DarkSeaGreen;
            }
            else
            {
                TabAlarm_TextBox_USB24.Text = "报警";
                TabAlarm_TextBox_USB24.BackColor = Color.LightCoral;
            }

        }
        #endregion

        #region 访问标签页消息响应
        private void TabAccess_Button_GetInfo_Click(object sender, EventArgs e)
        {
            GetPowerOutputValueMsgAll();
            GetPowerOutputSafeValueMsgAll();
            GetPowerOutputFreqMsgAll();
            GetPowerOutputSwitchMsgAll();
            GetTemAndHumValueMsgAll();
            GetTemAndHumSafeValueMsgAll();
            GetTemAndHumFreqMsgAll();
            GetWireTemValueMsgAll();
            GetWireTemSafeValueMsgAll();
            GetWireTemFreqMsgAll();
            GetElectricFreqMsgAll();
            GetDeviceFreqMsgAll();
            GetOutputSwitchMsgAll();
        }
        #endregion

        #region 控制标签页消息响应
        private void TabControl_Button_SetInfo_Click(object sender, EventArgs e)
        {
            SetTemAndHumSafeValueMsgAll();
            SetTemAndHumFreqMsgAll();
            SetWireTemSafeValueMsgAll();
            SetWireTemFreqMsgAll();
            SetOutputSwitchMsgAll();
            SetInputTurnMsgAll();
            SetElecFreqMsgAll();
            SetDevFreqMsgAll();
            SetPowerOutputSafeValueMsgAll();
            SetPowerOutputFreqMsgAll();
            SetPowerOutputSwitchMsgAll();
        }

        private void TabControl_Button_SetUSB_Click(object sender, EventArgs e)
        {
            SetUSBEnableMsgAll();
        }

        private void TabControl_Button_SetInput_Click(object sender, EventArgs e)
        {
            SetInputEnableMsgAll();
        }

        private void TabControl_Button_SetTemAndHum_Click(object sender, EventArgs e)
        {
            SetTemAndHumEnableMsgAll();
        }

        private void TabControl_Button_SetWater_Click(object sender, EventArgs e)
        {
            SetWaterLeachEnableMsgAll();
        }

        #endregion
    }
}
