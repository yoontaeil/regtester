using System;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace LineChartTEST
{
    public partial class Form1 : Form
    {
        //[2022.10.01] : github용 test변수 추가
        string yti_test = "";
        
        DateTime start;                                 //Measure start time
        DateTime end;                                   //Measure end time
               
        private List<double> _customValueList;

        string filterString = "";
        bool scrolling = true;
        Color receivedColor = Color.Green;
        Color sentColor = Color.Blue;
        private Line partialLine = null;
        public string PATH = "";                        //File save path
        public double RESULT = 0;
        public int Zoom_Cnt = 0;

        public double MAX = 0;                          //max
        public double MIN = 9999999999;                 //min
        public double SUM = 0;                          //sum
        public double AVG = 0;                          //average

        public bool AUTO = false;                       //false : Manual, true : Auto
        public string TIME = "";                        //time
        public string Formula = "";                     //농도
        public string Back_data = "";                   //단위환산전의 값        

        private class Line
        {
            public string Str;
            public Color ForeColor;

            public Line(string str, Color color)
            {
                Str = str;
                ForeColor = color;
            }
        };

        ArrayList lines = new ArrayList();
        
        public string Mid(string sString, int nStart, int nLength)
        {
            string sReturn;
            
            --nStart;

            if (nStart <= sString.Length)
            {
                if ((nStart + nLength) <= sString.Length)
                {             
                    sReturn = sString.Substring(nStart, nLength);
                }
                else
                {                 
                    sReturn = sString.Substring(nStart);
                }
            }
            else
            {            
                sReturn = string.Empty;
            }
            return sReturn;
        }

        public new string Left(string sString, int nLength)
        {
            string sReturn;

            if (nLength > sString.Length) nLength = sString.Length;
            sReturn = sString.Substring(0, nLength);
            return sReturn;
        }

        public new string Right(string sString, int nLength)
        {
            string sReturn;

            if (nLength > sString.Length) nLength = sString.Length;
            sReturn = sString.Substring(sString.Length - nLength, nLength);
            return sReturn;
        }

        public Form1()
        {            
            string tmp = "";
            string sfilename = "C:\\RGTEST_DATA\\RG_SET.mset";

            InitializeComponent();
                      
            chart2.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
            chart2.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart2.Series[0].MarkerStyle = MarkerStyle.None;

            tbUpdateInterval.Text = "500";
            timer1.Interval = 500;            
           
            _customValueList = new List<double>();
            
            DirectoryInfo di = new DirectoryInfo("C:\\RGTEST_DATA");
            if (!di.Exists) di.Create();
            tbPath.Text = "C:\\RGTEST_DATA";

            //================================================================== read data start            
            try
            {
                StreamReader Reader = new StreamReader(sfilename, false);

                //그림 형식            
                tmp = Reader.ReadLine();
                switch (tmp)
                {
                    case "0":
                        radiobtn_Image.Checked = true;
                        break;

                    case "1":
                        radioButton2.Checked = true;
                        break;

                    case "2":
                        radioButton3.Checked = true;
                        break;

                    default:
                        radioButton4.Checked = true;
                        break;
                }                

                //path
                tmp = Reader.ReadLine();
                PATH = tmp;
                tbPath.Text = tmp;

                //interval
                tmp = Reader.ReadLine();
                timer1.Interval = Convert.ToInt16(tmp);
                tbUpdateInterval.Text = tmp;

                //a
                tmp = Reader.ReadLine();
                tb_a.Text = tmp;

                //b
                tmp = Reader.ReadLine();
                tb_b.Text = tmp;

                //marker
                tmp = Reader.ReadLine();
                if (tmp == "1")
                {
                    chart2.Series[0].MarkerStyle = MarkerStyle.Circle;
                    checkBox4.Checked = true;
                }
                else
                {
                    chart2.Series[0].MarkerStyle = MarkerStyle.None;
                    checkBox4.Checked = false;
                }

                Reader.Close();
                Reader = null;
            }
            catch
            {

            }
            //================================================================== read data end
            AUTO = true;
            //comport            
            Settings.Read();
            CommPort com = CommPort.Instance;
            com.StatusChanged += OnStatusChanged;
            com.DataReceived += OnDataReceived;
            com.Open();
        }

        protected void MouseWheelOnChart(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            var yAxis = chart.ChartAreas[0].AxisY;
            double y, yMin, yMax, yMin2, yMax2;
            yMin = yAxis.ScaleView.ViewMinimum;
            yMax = yAxis.ScaleView.ViewMaximum;
            y = yAxis.PixelPositionToValue(e.Location.Y);
            if (e.Delta < 0)
            {
                yMin2 = y - (y - yMin) / 0.9;
                yMax2 = y + (yMax - y) / 0.9;
            }
            else
            {
                yMin2 = y - (y - yMin) * 0.9;
                yMax2 = y + (yMax - y) * 0.9;
            }
            //if (yMax2 > 5) yMax2 = 5;
            //if (yMin2 < 0) yMin2 = 0;
            chart2.ChartAreas[0].AxisY.Minimum = yMin2;
            chart2.ChartAreas[0].AxisY.Maximum = yMax2;                        
        }

        private string PrepareData(string StringIn)
        {
            // The names of the first 32 characters
            string[] charNames = { "NUL", "SOH", "STX", "ETX", "EOT",
                "ENQ", "ACK", "BEL", "BS", "TAB", "LF", "VT", "FF", "CR", "SO", "SI",
                "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB",
                "ESC", "FS", "GS", "RS", "US", "Space"};

            string StringOut = "";

            foreach (char c in StringIn)
            {
                if (Settings.Option.HexOutput)
                {
                    StringOut = StringOut + string.Format("{0:X2} ", (int)c);
                }
                else if (c < 32 && c != 9)
                {
                    //StringOut = StringOut + "<" + charNames[c] + ">";

                    //Uglier "Termite" style
                    //StringOut = StringOut + String.Format("[{0:X2}]", (int)c);
                }
                else
                {
                    StringOut = StringOut + c;
                }
            }
            return StringOut;
        }

        internal delegate void StringDelegate(string data);

        public void OnDataReceived(string dataIn)
        {            
            //Handle multi-threading
            if (InvokeRequired)
            {
                Invoke(new StringDelegate(OnDataReceived), new object[] { dataIn });
                return;
            }

            // pause scrolling to speed up output of multiple lines
            bool saveScrolling = scrolling;
            scrolling = false;
            
            // if we detect a line terminator, add line to output
            int index;
            try
            {
                while (dataIn.Length > 0 && ((index = dataIn.IndexOf("\r")) != -1 || (index = dataIn.IndexOf("\n")) != -1))
                {
                    string StringIn = dataIn.Substring(0, index);
                    dataIn = dataIn.Remove(0, index + 1);
                 
                    RESULT = Convert.ToDouble(dataIn);
                    chart2.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(tbMAX.Text);
                    chart2.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(tbMIN.Text);
                    chart2.ChartAreas[0].AxisY.Interval = Convert.ToDouble(tbINT.Text);
                    logFile_writeLine(AddData(StringIn).Str);

                    //
                    if (RESULT > 1000000)
                    {
                        RESULT = RESULT / 1000000;
                    }
                    else if (RESULT > 1000)
                    {
                        RESULT = RESULT / 1000;
                    }
                    else if (RESULT > 1)
                    {
                        RESULT = RESULT;
                    }
                    else if (RESULT > 0.001)
                    {
                        RESULT = RESULT * 1000;
                    }
                    else if (RESULT > 0.000001)
                    {
                        RESULT = RESULT * 1000000;
                    }
                    else if (RESULT > 0.000000001)
                    {
                        RESULT = RESULT * 1000000000;
                    }
                    else if (RESULT > 0.000000000001)
                    {
                        RESULT = RESULT * 1000000000000;
                    }
                    //

                    _customValueList.Add(RESULT);
                    UpdateSecondChart();                            //chart value add  
                    partialLine = null; // terminate partial line                         
                    break;                    
                }
                // if we have data remaining, add a partial line
                if (dataIn.Length > 0) partialLine = AddData(dataIn);

                // restore scrolling
                //scrolling = saveScrolling;
                scrolling = true;
                outputList_Scroll();
            }
            catch
            {

            }                          
        }

        void outputList_Scroll()
        {
            if (scrolling)
            {
                int itemsPerPage = outputList.Height / outputList.ItemHeight;
                outputList.TopIndex = outputList.Items.Count - itemsPerPage;
            }
        }

        bool outputList_ApplyFilter(string s)
        {
            if (filterString == "")
            {
                return true;
            }
            else if (s == "")
            {
                return false;
            }
            else if (Settings.Option.FilterUseCase)
            {
                return (s.IndexOf(filterString) != -1);
            }
            else
            {
                string upperString = s.ToUpper();
                string upperFilter = filterString.ToUpper();
                return (upperString.IndexOf(upperFilter) != -1);
            }
        }

        void outputList_Update(Line line)
        {
            // should we add to output?
            if (outputList_ApplyFilter(line.Str))
            {
                if (line.Str != "S" && line.Str != "C" && line.Str != "R1" && line.Str != "R2" && line.Str != "R3" && line.Str != "R4" && line.Str != "M" && line.Str != "N")
                {
                    // is the line already displayed?
                    bool found = false;
                    for (int i = 0; i < outputList.Items.Count; ++i)
                    {
                        int index = (outputList.Items.Count - 1) - i;
                        if (line == outputList.Items[index])
                        {
                            // is item visible?
                            int itemsPerPage = outputList.Height / outputList.ItemHeight;
                            if (index >= outputList.TopIndex && index < (outputList.TopIndex + itemsPerPage))
                            {                                
                                outputList.Refresh();
                            }
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        // not found, so add it
                        outputList.Items.Add(line);
                    }
                }                
            }
        }

        private Line AddData(string StringIn)
        {
            string StringOut = PrepareData(StringIn);

            // if we have a partial line, add to it.
            if (partialLine != null)
            {
                if (StringOut != "S" && StringOut != "C" && StringOut != "R1" && StringOut != "R2" && StringOut != "R3" && StringOut != "R4" && StringOut != "M" && StringOut != "N")                    
                {
                    // tack it on
                    partialLine.Str = partialLine.Str + StringOut;                 
                }                
                outputList_Update(partialLine);
                return partialLine;
            }
            return outputList_Add(StringOut, receivedColor);
        }

        Line outputList_Add(string str, Color color)
        {         
            string backup_data = "";
            ListViewItem Item;
            int Reg_value = 0;
            double TMP_VAL = 0;
            double L_VAL = 0;
            Line newLine = new Line(str, color);            
            lines.Add(newLine);

            if (outputList_ApplyFilter(newLine.Str))
            {                
                if (newLine.Str != "S" && newLine.Str != "C" && newLine.Str != "R1" && newLine.Str != "R2" && newLine.Str != "R3" && newLine.Str != "R4" && newLine.Str != "M" && newLine.Str != "N")                               //data인 경우
                {
                    DateTime NOWTIME = DateTime.Now;
                    string TIME_str = NOWTIME.Year + "." + NOWTIME.Month + "." + NOWTIME.Day + "." + NOWTIME.Hour + ":" + NOWTIME.Minute + ":" + NOWTIME.Second;
                    if (rd_R.Checked == true)                                               //저항
                    {
                        newLine.Str = string.Format("{0:0.00}", double.Parse(newLine.Str));
                        TMP_VAL = Convert.ToDouble(newLine.Str);                        
                        //MAX
                        if (TMP_VAL > MAX)
                        {
                            MAX = TMP_VAL;
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX);
                        }
                        //MIN
                        if (TMP_VAL < MIN)
                        {
                            MIN = TMP_VAL;                            
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN);
                        }
                        //AVERAGE
                        SUM += TMP_VAL;
                        AVG = SUM / Convert.ToInt16(lblCNT.Text);
                        lbl_AVG.Text = string.Format("{0:0.000}", double.Parse(AVG.ToString()));
                        
                        backup_data = newLine.Str;
                        if (newLine.Str == "-1000")
                        {
                            newLine.Str = "OVER";
                        }
                        else if (TMP_VAL > 99999999.99)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL / 1000000);    //100~999.99Mohm  
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " MOhm";    
                            chart2.Series[0].Name = "Mohm";                           
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX / 1000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN / 1000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG/1000000).ToString()));
                        }
                        else if (TMP_VAL > 9999999.99)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL / 1000000);    //10~99.9Mohm    
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " MOhm";        
                            chart2.Series[0].Name = "Mohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX / 1000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN / 1000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG / 1000000).ToString()));
                        }
                        else if (TMP_VAL > 999999.99)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL / 1000000);     //1~9.99Mohm                            
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " MOhm";                               
                            chart2.Series[0].Name = "Mohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX / 1000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN / 1000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG / 1000000).ToString()));
                        }
                        else if (TMP_VAL > 99999.99)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL / 1000);        //100~999.9kohm                            
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " kOhm";                                  
                            chart2.Series[0].Name = "kohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX / 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN / 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG / 1000).ToString()));
                        }
                        else if (TMP_VAL > 9999.9)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL / 1000);        //10~99.9kohm                            
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " kOhm";                                  
                            chart2.Series[0].Name = "kohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX / 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN / 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG / 1000).ToString()));
                        }
                        else if (TMP_VAL > 999.99)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL / 1000);        //1~9.99kohm                           
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " kOhm";                                 
                            chart2.Series[0].Name = "kohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX / 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN / 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG / 1000).ToString()));
                        }
                        else if (TMP_VAL > 99.999)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL);               //100~999.99ohm                               
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " Ohm";                                            
                            chart2.Series[0].Name = "ohm";
                        }
                        else if (TMP_VAL > 9.9999)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL);               //10~99.999ohm                           
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " Ohm";                                      
                            chart2.Series[0].Name = "ohm";
                        }
                        else if (TMP_VAL > 0.9999)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL);               //1~9.999ohm                                
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " Ohm";                                             
                            chart2.Series[0].Name = "ohm";
                        }
                        else if (TMP_VAL > 0.09999)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL * 1000);       //0.1~0.999ohm                              
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " mOhm";                                    
                            chart2.Series[0].Name = "mohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000).ToString()));
                        }
                        else if (TMP_VAL > 0.009999)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL * 1000);       //0.1~0.999ohm                               
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " mOhm";                                
                            chart2.Series[0].Name = "mohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000).ToString()));
                        }
                        else if (TMP_VAL > 0.0009999)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL * 1000);       //0.01~0.0999ohm                           
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " mOhm";                                             
                            chart2.Series[0].Name = "mohm";                            
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000).ToString()));
                        }

                        Back_data = newLine.Str;            //단위포함 data                            
                        Formula = Left(Convert.ToString((Convert.ToDouble(backup_data) - Convert.ToDouble(tb_b.Text)) / Convert.ToDouble(tb_a.Text)), 8);                        
                        newLine.Str = newLine.Str + "   " + string.Format("{0:0.00000}", Formula) + "   " + TIME + "   " + TIME_str;

                        if (AUTO == true)
                        {                            
                            chart2.ChartAreas[0].AxisY.Maximum = Math.Round(L_VAL + (Convert.ToDouble(lbl_MAX.Text)),2);
                            chart2.ChartAreas[0].AxisY.Minimum = Math.Round(L_VAL - (Convert.ToDouble(lbl_MAX.Text)),2);
                            chart2.ChartAreas[0].AxisY.Interval = Math.Round((((L_VAL + (Convert.ToDouble(lbl_MAX.Text))) - (L_VAL - (Convert.ToDouble(lbl_MAX.Text)))) / 4),2);
                        }
                        else
                        {
                            chart2.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(tbMAX.Text);
                            chart2.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(tbMIN.Text);
                            chart2.ChartAreas[0].AxisY.Interval = Convert.ToDouble(tbINT.Text);
                        }                        
                    }
                    else
                    {
                        newLine.Str = string.Format("{0:0.00000000}", double.Parse(newLine.Str));
                        TMP_VAL = Convert.ToDouble(newLine.Str);
                        //MAX
                        if (TMP_VAL > MAX)
                        {
                            MAX = TMP_VAL;
                            lbl_MAX.Text = string.Format("{0:0.00000000}", TMP_VAL);
                        }
                        //MIN
                        if (TMP_VAL < MIN)
                        {
                            MIN = TMP_VAL;
                            lbl_MIN.Text = string.Format("{0:0.00000000}", TMP_VAL);
                        }
                        //AVERAGE
                        SUM += TMP_VAL;
                        AVG = SUM / Convert.ToInt16(lblCNT.Text);
                        lbl_AVG.Text = string.Format("{0:0.00000000}", double.Parse(AVG.ToString()));
                        
                        backup_data = newLine.Str;

                        if (newLine.Str == "-1000")
                        {
                            newLine.Str = "OVER";
                        }                        
                        else if (TMP_VAL > 99.999)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " A";
                            Reg_value = 10;

                            chart2.Series[0].Name = "A";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG).ToString()));
                        }
                        else if (TMP_VAL > 9.9999)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " A";
                            Reg_value = 10;

                            chart2.Series[0].Name = "A";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG).ToString()));
                        }
                        else if (TMP_VAL > 0.9999)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " A";
                            Reg_value = 10;

                            chart2.Series[0].Name = "A";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG).ToString()));
                        }
                        else if (TMP_VAL > 0.09999)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL * 1000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " mA";
                            Reg_value = 10;

                            chart2.Series[0].Name = "mA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG*1000).ToString()));
                        }
                        else if (TMP_VAL > 0.009999)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL * 1000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " mA";
                            Reg_value = 10;

                            chart2.Series[0].Name = "mA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000).ToString()));
                        }
                        else if (TMP_VAL > 0.0009999)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL * 1000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " mA";
                            Reg_value = 1000;

                            chart2.Series[0].Name = "mA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000).ToString()));
                        }
                        else if (TMP_VAL > 0.00009999)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL * 1000000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " uA";
                            Reg_value = 1000;

                            chart2.Series[0].Name = "uA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000000).ToString()));
                        }
                        else if (TMP_VAL > 0.000009999)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL * 1000000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " uA";
                            Reg_value = 100000;

                            chart2.Series[0].Name = "uA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000000).ToString()));
                        }
                        else if (TMP_VAL > 0.0000009999)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL * 1000000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " uA";
                            Reg_value = 100000;

                            chart2.Series[0].Name = "uA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000000).ToString()));
                        }
                        else if (TMP_VAL > 0.00000009999)
                        {
                            newLine.Str = string.Format("{0:000.00}", TMP_VAL * 1000000000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " nA";
                            Reg_value = 10000000;

                            chart2.Series[0].Name = "nA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000000000).ToString()));
                        }
                        else if (TMP_VAL > 0.000000009999)
                        {
                            newLine.Str = string.Format("{0:00.000}", TMP_VAL * 1000000000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " nA";
                            Reg_value = 10000000;

                            chart2.Series[0].Name = "nA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000000000).ToString()));
                        }
                        else if (TMP_VAL > 0.0000000009999)
                        {
                            newLine.Str = string.Format("{0:0.0000}", TMP_VAL * 1000000000);
                            L_VAL = Convert.ToDouble(newLine.Str);
                            newLine.Str = newLine.Str + " nA";
                            Reg_value = 10000000;

                            chart2.Series[0].Name = "nA";
                            lbl_MAX.Text = string.Format("{0:0.000}", MAX * 1000000000);
                            lbl_MIN.Text = string.Format("{0:0.000}", MIN * 1000000000);
                            lbl_AVG.Text = string.Format("{0:0.000}", double.Parse((AVG * 1000000000).ToString()));
                        }

                        Back_data = newLine.Str;

                        //공식 : (((((기준전압3V - (측정전압 * 저항1kohm)) * 저항1kohm) / (측정전압 * 저항1kohm)) - b) / a                        
                        Formula = Left(Convert.ToString((((((3 - (Convert.ToDouble(backup_data) * Reg_value)) * Reg_value) / (Convert.ToDouble(backup_data) * Reg_value)) - Convert.ToDouble(tb_b.Text)) / Convert.ToDouble(tb_a.Text))), 8);                        
                        newLine.Str = newLine.Str + "   " + string.Format("{0:0.00000}",Formula) + "   " + TIME + "   " + TIME_str;

                        if (AUTO == true)
                        {
                            chart2.ChartAreas[0].AxisY.Maximum = Math.Round(L_VAL + (Convert.ToDouble(lbl_MAX.Text)), 2);
                            chart2.ChartAreas[0].AxisY.Minimum = Math.Round(L_VAL - (Convert.ToDouble(lbl_MAX.Text)), 2);
                            chart2.ChartAreas[0].AxisY.Interval = Math.Round((((L_VAL + (Convert.ToDouble(lbl_MAX.Text))) - (L_VAL - (Convert.ToDouble(lbl_MAX.Text)))) / 4), 2);
                        }
                        else
                        {
                            chart2.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(tbMAX.Text);
                            chart2.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(tbMIN.Text);
                            chart2.ChartAreas[0].AxisY.Interval = Convert.ToDouble(tbINT.Text);
                        }
                        //if (AUTO == true)
                        //{
                        //    chart2.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(MAX + (MAX * 0.05));
                        //    chart2.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(MIN - (MIN * 0.05));
                        //    chart2.ChartAreas[0].AxisY.Interval = Convert.ToDouble(((MAX + (MAX * 0.05)) - (MIN - (MIN * 0.05))) / 4);                            
                        //}
                        //else
                        //{
                        //    chart2.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(tbMAX.Text);
                        //    chart2.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(tbMIN.Text);
                        //    chart2.ChartAreas[0].AxisY.Interval = Convert.ToDouble(tbINT.Text);
                        //}                        
                    }
                    outputList.Items.Add(newLine);
                    listBox1.Items.Add(backup_data + "," + Formula + "," + TIME + "," + TIME_str);
                    outputList_Scroll();

                    Item = new ListViewItem(Back_data);
                    Item.SubItems.Add(Formula);
                    Item.SubItems.Add(TIME);
                    Item.SubItems.Add(TIME_str);
                    listView1.Items.Add(Item);

                    listView1.EnsureVisible(listView1.Items.Count - 1);
                }                
            }                    
            return newLine;
        }

        public void logFile_writeLine(string stringOut)
        {
            if (Settings.Option.LogFileName != "")
            {
                Stream myStream = File.Open(Settings.Option.LogFileName,
                    FileMode.Append, FileAccess.Write, FileShare.Read);
                if (myStream != null)
                {
                    StreamWriter myWriter = new StreamWriter(myStream, Encoding.UTF8);
                    myWriter.WriteLine(stringOut);
                    myWriter.Close();
                }
            }
        }

        public void OnStatusChanged(string status)
        {
            //Handle multi-threading
            if (InvokeRequired)
            {
                Invoke(new StringDelegate(OnStatusChanged), new object[] { status });
                return;
            }

            textBox1.Text = status;
        }
                     
        private void btnAdd_Click(object sender, EventArgs e)
        {
            btnAdd.Enabled = false;
            chart2.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart2.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
            if (outputList.Items.Count == 0)
            {
                start = DateTime.Now;
            }
            CommPort com = CommPort.Instance;
            if (rd_MANUAL.Checked == true)
            {
                if (rd_R.Checked == true)
                {
                    com.Send("M");
                }
                else
                {
                    com.Send("N");
                }
            }
            else
            {
                if (rd_R.Checked == true)
                {
                    com.Send("S");
                }
                else
                {
                    com.Send("C");
                }
            }            
            Thread.Sleep(400);            
            btnAdd.Enabled = true;
        }
        public uint ii = 1;
        private void UpdateSecondChart()
        {                        
            end = DateTime.Now;
            string strtmp = (end - start).ToString();                        
            TIME = Left(strtmp,8);
            chart2.Series[0].Points.AddXY(TIME, _customValueList[_customValueList.Count - 1]);
            lblCNT.Text = (ii).ToString();
            ii++;
            chart2.Invalidate();            
        }

        private void btn_Data_Click(object sender, EventArgs e)
        {
            int i = 0;
            DateTime NOWTIME = DateTime.Now;
            string TIME_str = NOWTIME.Year + "_" + NOWTIME.Month + "_" + NOWTIME.Day + "_" + NOWTIME.Hour + "_" + NOWTIME.Minute + "_" + NOWTIME.Second + "_" + NOWTIME.Millisecond;
           
            StreamWriter SaveFile = new StreamWriter(PATH + "\\" + TIME_str + ".csv");             
            SaveFile.WriteLine("RT-100 tester");
            SaveFile.WriteLine("");
            SaveFile.WriteLine("Count," + lblCNT.Text);
            SaveFile.WriteLine("MAX," + "=MAX(B10:B" + (listBox1.Items.Count + 9) + ")" + "," + "=MAX(C10:C" + (listBox1.Items.Count + 9) + ")");
            SaveFile.WriteLine("MIN," + "=MIN(B10:B" + (listBox1.Items.Count + 9) + ")" + "," + "=MIN(C10:C" + (listBox1.Items.Count + 9) + ")");
            SaveFile.WriteLine("AVERAGE," + "=AVERAGE(B10:B" + (listBox1.Items.Count + 9) + ")" + "," + "=AVERAGE(C10:C" + (listBox1.Items.Count + 9) + ")");                        
            if (rd_R.Checked == true)
            {
                SaveFile.WriteLine("DATA," + "Resistance");
            }
            else
            {
                SaveFile.WriteLine("DATA," + "Current");
            }
            SaveFile.WriteLine("");
            SaveFile.WriteLine("NO" + "," + "DATA" + "," + "Concentration" + "," + "TIME" + "," + "NOW");
            
            foreach (var item in listBox1.Items)
            {
                i++;
                SaveFile.WriteLine(i + "," + item + ",");
                SaveFile.ToString();
            }
            SaveFile.Close();

            MessageBox.Show("Data Saved!!", "NOTICE", MessageBoxButtons.OK);            
        }

        private void btnDeserialize_Click(object sender, EventArgs e)
        {            
            string filePath = PATH + "\\ChartData_Stream.xml";
            FileStream stream = new FileStream(filePath, FileMode.Open);
            chart2.Serializer.IsResetWhenLoading = true;
            chart2.Serializer.Load(stream);

            stream.Close();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            chart2.Series[0].Points.Clear();
            ii = 1;
        }
        
        private void btnFilePathSe_Click(object sender, EventArgs e)
        {
            try
            {                
                string filePath = PATH + "\\ChartData_FilePath.xml";
                if (File.Exists(filePath))
                {                    
                    File.Copy(filePath, PATH + "\\ChartData_FilePath.bak", true);
                    File.Delete(filePath);
                }
                
                chart2.Serializer.Content = SerializationContents.Default;
                chart2.Serializer.Format = SerializationFormat.Xml;
                chart2.Serializer.Save(filePath);                
            }
            catch (Exception exc)
            {
                MessageBox.Show("An exception occurred.\nPlease try again.");
            }
        }

        private void btnFilePathDe_Click(object sender, EventArgs e)
        {
            string filePath = Application.StartupPath + "\\ChartData_FilePath.xml";
            chart2.Serializer.Reset();
            chart2.Serializer.Load(filePath);            
        }

        //private void btn_Image_Click(object sender, EventArgs e)
        //{
        //    chart2.SaveImage(PATH + "AAA.BMP", ChartImageFormat.Bmp);           //파일 이름 형식 정한다. ex)yyymmddss.bmp
        //}

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button2.Enabled = true;

            chart2.ChartAreas[0].Area3DStyle.Enable3D = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button3.Enabled = true;
            button2.Enabled = false;   
            
            chart2.ChartAreas[0].Area3DStyle.Enable3D = true;         
        }
                
        private void button5_Click(object sender, EventArgs e)
        {
            lines.Clear();            
            listView1.Items.Clear();
            partialLine = null;
            MIN = 9999999999;            
            lbl_MIN.Text = "";

            MAX = 0;
            lbl_MAX.Text = "";

            AVG = 0;
            lbl_AVG.Text = "";

            SUM = 0;

            lblCNT.Text = "";
            ii = 1;
            outputList.Items.Clear();
            listBox1.Items.Clear();
        }       
        
        private void btn_Exit_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you want exit program?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {                
                System.Diagnostics.Process.GetCurrentProcess().Kill();             
            }
            else
            {
                return;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {            
            CommPort com = CommPort.Instance;
            if (rd_MANUAL.Checked == true)
            {
                if (rd_R.Checked == true)
                {
                    com.Send("M");
                }
                else
                {
                    com.Send("N");
                }
            }
            else
            {
                if (rd_R.Checked == true)
                {
                    com.Send("S");
                }
                else
                {
                    com.Send("C");
                }
            }
            Thread.Sleep(400);
            //Application.DoEvents();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                Axis ax = chart2.ChartAreas[0].AxisX;                           //chart x
                Axis ay = chart2.ChartAreas[0].AxisY;                           //chart y
                ax.ScaleView.ZoomReset(Zoom_Cnt);                               //zoomout x
                ay.ScaleView.ZoomReset(Zoom_Cnt);                               //zoomout y                
                Zoom_Cnt = 0;                                                   //zoom count clear
                chart2.ChartAreas[0].CursorX.IsUserSelectionEnabled = false;
                chart2.ChartAreas[0].CursorY.IsUserSelectionEnabled = false;
                button4.Enabled = false;
                checkBox2.Checked = false;
                timer1.Enabled = true;
                label1.Text = "START";
                tbMAX.Enabled = false;
                tbMIN.Enabled = false;
                tbINT.Enabled = false;
                label1.BackColor = Color.YellowGreen;                
                if (outputList.Items.Count == 0) start = DateTime.Now;
            }                 
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                chart2.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
                chart2.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
                button4.Enabled = true;
                checkBox1.Checked = false;
                timer1.Enabled = false;
                label1.Text = "STOP";
                tbMAX.Enabled = true;
                tbMIN.Enabled = true;
                tbINT.Enabled = true;
                label1.BackColor = Color.White;
            }            
        }        

        private void btnUpdateInterval_Click(object sender, EventArgs e)
        {
            int interval = 0;
            if (int.TryParse(tbUpdateInterval.Text, out interval))
            {
                if (interval >= 500)
                {
                    timer1.Interval = interval;
                }                    
                else
                {
                    MessageBox.Show("The data should be more than 500mS");
                }                    
            }
            else
            {
                MessageBox.Show("Inappropriate data.");
            }
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            FolderBrowserDialog F = new FolderBrowserDialog();
            F.ShowDialog();
            tbPath.Text = F.SelectedPath;
            PATH = F.SelectedPath;
            F = null;
        }

        private void button8_Click_1(object sender, EventArgs e)
        {            
            //text                            
            string File_name = "C:\\RGTEST_DATA\\RG_SET.mset";

            StreamWriter Writer = new StreamWriter(File_name, false, Encoding.Default);
            
            //그림 형식
            if (radiobtn_Image.Checked)
            {
                Writer.WriteLine("0");
            }
            else if (radioButton2.Checked)
            {
                Writer.WriteLine("1");
            }
            else if (radioButton3.Checked)
            {
                Writer.WriteLine("2");
            }
            else
            {
                Writer.WriteLine("3");
            }
            //경로
            Writer.WriteLine(tbPath.Text);
            //인터벌
            Writer.WriteLine(tbUpdateInterval.Text);
            //농도a
            Writer.WriteLine(tb_a.Text);
            //농도b
            Writer.WriteLine(tb_b.Text);

            //Marker
            if (checkBox4.Text == "ON")
            {
                Writer.WriteLine("1");
            }
            else
            {
                Writer.WriteLine("0");
            }
            Writer.Close();
            Writer = null;

            MessageBox.Show("Setting Data Saved!!", "NOTICE", MessageBoxButtons.OK);
        }
        
        private void button3_Click_1(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button2.Enabled = true;

            chart2.ChartAreas[0].Area3DStyle.Enable3D = false;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            button3.Enabled = true;
            button2.Enabled = false;

            chart2.ChartAreas[0].Area3DStyle.Enable3D = true;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked == true)
            {
                checkBox3.Text = "Manual";
                AUTO = false;
            }
            else
            {
                checkBox3.Text = "Auto";
                AUTO = true;
            }
        }

        private void btn_Image_Click_2(object sender, EventArgs e)
        {
            DateTime NOWTIME = DateTime.Now;
            string TIME_str = NOWTIME.Year + "_" + NOWTIME.Month + "_" + NOWTIME.Day + "_" + NOWTIME.Hour + "_" + NOWTIME.Minute + "_" + NOWTIME.Second + "_" + NOWTIME.Millisecond;
            if (Right(PATH, 1) != "\\") PATH = PATH + "\\";

            if (radiobtn_Image.Checked)
            {
                chart2.SaveImage(PATH + TIME_str + ".BMP", ChartImageFormat.Bmp);
            }
            else if (radioButton2.Checked)
            {
                chart2.SaveImage(PATH + TIME_str + ".GIF", ChartImageFormat.Gif);
            }
            else if (radioButton3.Checked)
            {
                chart2.SaveImage(PATH + TIME_str + ".JPEG", ChartImageFormat.Jpeg);
            }
            else
            {
                chart2.SaveImage(PATH + TIME_str + ".PNG", ChartImageFormat.Png);
            }
            MessageBox.Show("Picture data saved!!", "NOTICE", MessageBoxButtons.OK);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TopMost = false;

            Form2 form2 = new Form2();
            form2.ShowDialog();
        }

        private void rd_R_CheckedChanged(object sender, EventArgs e)
        {
            start = DateTime.Now;
            chart2.Series[0].Name = "ohm";
            chart2.Series[0].Points.Clear();
            listView1.Items.Clear();
            ii = 1;

            lines.Clear();
            partialLine = null;

            MIN = 9999999999;
            lbl_MIN.Text = "";

            MAX = 0;
            lbl_MAX.Text = "";

            AVG = 0;
            lbl_AVG.Text = "";

            SUM = 0;

            lblCNT.Text = "";

            outputList.Items.Clear();
            listBox1.Items.Clear();
        }

        private void rd_C_CheckedChanged(object sender, EventArgs e)
        {
            start = DateTime.Now;
            chart2.Series[0].Name = "Current";
            chart2.Series[0].Points.Clear();
            listView1.Items.Clear();
                        
            ii = 1;

            lines.Clear();
            partialLine = null;

            MIN = 9999999999;
            lbl_MIN.Text = "";

            MAX = 0;
            lbl_MAX.Text = "";

            AVG = 0;
            lbl_AVG.Text = "";

            SUM = 0;

            lblCNT.Text = "";

            outputList.Items.Clear();
            listBox1.Items.Clear();
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            CommPort com = CommPort.Instance;
            com.Send("R1");                     //10 ohm
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            CommPort com = CommPort.Instance;
            com.Send("R2");                     //1 kohm
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            CommPort com = CommPort.Instance;
            com.Send("R3");                     //100 kohm
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            CommPort com = CommPort.Instance;
            com.Send("R4");                     //1 Mohm
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rd_AUTO.Checked == true)
            {
                rd_R1.Enabled = false;
                rd_R2.Enabled = false;
                rd_R3.Enabled = false;
                rd_R4.Enabled = false;
            }
            else if (rd_MANUAL.Checked == true)
            {
                rd_R1.Enabled = true;
                rd_R2.Enabled = true;
                rd_R3.Enabled = true;
                rd_R4.Enabled = true;
            }
        }

        private void rd_AUTO_Click(object sender, EventArgs e)
        {
            rd_R1.Enabled = false;
            rd_R2.Enabled = false;
            rd_R3.Enabled = false;
            rd_R4.Enabled = false;
        }

        private void rd_MANUAL_Click(object sender, EventArgs e)
        {
            rd_R1.Enabled = true;
            rd_R2.Enabled = true;
            rd_R3.Enabled = true;
            rd_R4.Enabled = true;
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void lbl_AVG_Click(object sender, EventArgs e)
        {

        }

        private void chart2_MouseUp(object sender, MouseEventArgs e)
        {
            Zoom_Cnt++;
        }

        Point? prevPosition = null;
        ToolTip tooltip = new ToolTip();

        void chart2_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (prevPosition.HasValue && pos == prevPosition.Value)
                return;
            tooltip.RemoveAll();
            prevPosition = pos;
            var results = chart2.HitTest(pos.X, pos.Y, false,
                                            ChartElementType.DataPoint);
            foreach (var result in results)
            {
                //if (result.ChartElementType == ChartElementType.DataPoint)
                //{
                    var prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        var pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        var pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);

                    // check if the cursor is really close to the point (2 pixels around the point)
                    //if (Math.Abs(pos.X - pointXPixel) < 2 &&
                    //    Math.Abs(pos.Y - pointYPixel) < 2)
                    //{
                        //tooltip.Show("X=" + prop.XValue + ", Y=" + prop.YValues[0], this.chart2,
                        //                pos.X, pos.Y - 15);

                        tooltip.Show("Y=" + prop.YValues[0], this.chart2,
                                            pos.X, pos.Y - 15);
                    //}
                }
                //}
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked == true)
            {
                chart2.Series[0].MarkerStyle = MarkerStyle.Circle;
                checkBox4.Text = "ON";
            }
            else
            {
                chart2.Series[0].MarkerStyle = MarkerStyle.None;
                checkBox4.Text = "OFF";
            }
        }
    }
}