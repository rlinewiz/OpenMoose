// Decompiled with JetBrains decompiler
// Type: J2534.frmECMTECH
// Assembly: ECM-TECH Suite, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: B30416E2-D8D0-4434-82C3-04779BF15D87
// Assembly location: D:\Users\Vida\Desktop\ME7 suite\ME7 Suite.exe

using J2534.Checksums;
using J2534.Display;
using J2534.Flash;
using J2534.Flash.ECU;
using J2534.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace J2534
{
  public class frmMain : Form
  {
    private byte[] readout = new byte[0];
    private byte[] ramReadout = new byte[0];
    public readonly string HKCU_RUN = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    public readonly string APP_KEY = "Software\\" + Application.ProductName;
    private byte[] binFile;
    private int flashTime;
    private ToolComm dice;
    private ECUProgrammer eProg;
    private bool flash512;
    private bool p80;
    private long logTime;
    private ECULogger logger;
    private int numLogSessions;
    private frmMain.StateObjClass StateObj;
    private Utility.ModifyRegistry.ModifyRegistry regedit;
    [Obfuscation(Exclude = true, Feature = "default", StripAfterObfuscation = false)]
    private ECUParameters eParams;
    [Obfuscation(Exclude = true, Feature = "default", StripAfterObfuscation = false)]
    private ECUVariables eVars;
    private CultureInfo culture;
    private StreamWriter logFile;
    private IContainer components;
    private System.Windows.Forms.Timer flashTimer;
    private Button but_connectdice;
    private ComboBox comboBox_J2534_devices;
    private Button cmdDetectDevices;
    private MenuStrip menuMain;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ColorDialog colorDialog1;
    private System.Windows.Forms.Timer readTimer;
    private ToolStripMenuItem resetServiceReminderToolStripMenuItem;
    private TabControl tabControl1;
    private TabPage tabPage1;
    private ProgressBar progressFlash;
    private Button cmdRead;
    private Button cmdFlash;
    private Button cmdReset;
    private TextBox txtBin;
    private Label lblTime;
    private TabPage tabPage2;
    private CheckBox chkPSI;
    private GroupBox groupBox1;
    private Label vitals_custom;
    private Label label_custom;
    private Label vitals_retard;
    private Label label6;
    private Label vitals_fuelpressure;
    private Label label5;
    private Label vitals_lambda;
    private Label label3;
    private Label vitals_boost;
    private Label label1;
    private ComboBox comboBox_xmlparams;
    private Label lblLogTime;
    private CheckBox chkshowvitals;
    private Button cmdStopLogging;
    private Button cmdStartLogging;
    private Button cmdChooseLog;
    private TextBox txtParamsFile;
    private TextBox txtLogFile;
    private Button but_chooseparams;
    private System.Windows.Forms.Timer logTimer;
        private Label label2;
        private ToolStripMenuItem aboutToolStripMenuItem;

    public frmMain()
    {
      this.InitializeComponent();
      this.culture = CultureInfo.CreateSpecificCulture("en-GB");
      Thread.CurrentThread.CurrentCulture = this.culture;
      Thread.CurrentThread.CurrentUICulture = this.culture;
      this.eParams = new ECUParameters();
      this.eVars = new ECUVariables();
      this.dice = new ToolComm();
    }

    private void openBINFile()
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = "Volvo ME7 BIN File (*.bin)|*.bin";
      if (openFileDialog.ShowDialog() != DialogResult.OK)
        return;
      long num1 = new FileInfo(openFileDialog.FileName).Length / 1024L;
      switch (num1)
      {
        case 512:
        case 1024:
          this.txtBin.Text = openFileDialog.FileName;
          this.flash512 = num1 == 512L;
          try
          {
            VolvoChecksumUpdater volvoChecksumUpdater = new VolvoChecksumUpdater(openFileDialog.FileName);
            if (!volvoChecksumUpdater.updateChecksums(true))
            {
              if (!volvoChecksumUpdater.updateChecksums(false))
              {
                int num2 = (int) MessageBox.Show("Unknown error updating checksums!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                Environment.Exit(3);
              }
              else
              {
                int num3 = (int) MessageBox.Show("Checksums updated!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
              }
            }
            this.binFile = File.ReadAllBytes(openFileDialog.FileName);
            break;
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex.ToString());
            break;
          }
        default:
          int num4 = (int) MessageBox.Show("Incorrect File Size!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
          break;
      }
    }

    private void saveBINFile()
    {
      SaveFileDialog saveFileDialog = new SaveFileDialog();
      saveFileDialog.Filter = "ECU Binary (*.bin)|*.bin";
      if (saveFileDialog.ShowDialog() != DialogResult.OK)
        return;
      this.txtBin.Text = saveFileDialog.FileName;
    }

    private void flashTimer_Tick(object sender, EventArgs e)
    {
      this.lblTime.Text = "Flash Time: " + this.flashTime.ToString();
      lock (new object())
      {
        this.progressFlash.Value = this.flashTime > this.progressFlash.Maximum ? this.progressFlash.Maximum : this.flashTime;
        if (this.eProg.doneFlashing)
        {
          this.flashTimer.Stop();
          this.changeButtonState(true);
          this.lblTime.Text = "Done Flashing!";
          this.progressFlash.Value = 0;
        }
      }
      ++this.flashTime;
    }

    private void cmdFlash_Click(object sender, EventArgs e)
    {
      if (!this.dice.DeviceOpen)
      {
        int num1 = (int) MessageBox.Show("Please connect the J2534 Cable first.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
        this.openBINFile();
        if (this.txtBin.Text == "")
        {
          int num2 = (int) MessageBox.Show("Please choose a BIN file.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        else if (MessageBox.Show("Is the key in pos II with the engine NOT running?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
          this.txtBin.Text = "";
        }
        else
        {
          this.flashTime = 0;
          this.changeButtonState(false);
          int length = this.binFile.Length;
          if (this.binFile[length - 1] != (byte) 131 || this.binFile[length - 2] != (byte) 131)
          {
            int num3 = (int) MessageBox.Show("This file is corrupt. Please contact the distributor.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            this.changeButtonState(true);
            this.txtBin.Text = "";
          }
          else
          {
            if (this.flash512)
              this.progressFlash.Maximum = 56;
            this.eProg = new ECUProgrammer(this.dice, VolvoECUCommands.sbl, this.binFile, this.flash512, this.p80);
            this.eProg.startFlash();
            this.flashTimer.Start();
          }
        }
      }
    }

    private void cmdDetectDevices_Click(object sender, EventArgs e)
    {
      this.updateDevices();
    }

    private void updateDevices()
    {
      List<J2534Device> installedDevices = ToolComm.getInstalledDevices();
      if (installedDevices.Count == 0)
      {
        int num = (int) MessageBox.Show("Could not find any installed J2534 devices.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
        this.comboBox_J2534_devices.DataSource = (object) installedDevices;
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      this.updateDevices();
      this.regedit = new Utility.ModifyRegistry.ModifyRegistry();
      this.txtParamsFile.Text = this.getECUParams();
      try
      {
        if (this.txtParamsFile.Text.Equals(""))
          return;
        if (this.txtParamsFile.Text.Split('.')[this.txtParamsFile.Text.Split('.').Length - 1].Equals("xml"))
        {
          this.parseParameters();
          this.SetComboXML();
        }
        else
        {
          this.txtParamsFile.Text = "";
          this.setECUParams("");
        }
      }
      catch (Exception ex)
      {
        this.txtParamsFile.Text = "";
        this.setECUParams("");
        Console.WriteLine(ex.ToString());
      }
    }

    public void processReqs(string msgs)
    {
      foreach (ECUVariable eVar in (List<ECUVariable>) this.eVars)
      {
        try
        {
          if (eVar.word)
          {
            string input = msgs.Substring(0, 4);
            msgs = msgs.Substring(4);
            eVar.value = eVar.getHexValueFromString(input);
          }
          else
          {
            string input = msgs.Substring(0, 2);
            msgs = msgs.Substring(2);
            eVar.value = eVar.getHexValueFromString(input);
          }
        }
        catch (Exception ex)
        {
          eVar.value = (ushort) 0;
          ex.ToString();
        }
      }
    }

    private void cmdStopLogging_Click(object sender, EventArgs e)
    {
      this.dice.sendMsg(new CANPacket(ECULoggingCommands.msgCANRequestRecordSetStop), CANChannel.HS);
      if (this.logger.hs_Logging)
        this.logger.recs_req = false;
      this.logFile.Close();
      string[] strArray = this.txtLogFile.Text.Split(new string[1]
      {
        ".csv"
      }, StringSplitOptions.None);
      if (strArray.Length != 0)
        this.txtLogFile.Text = strArray[0] + "_" + (object) this.numLogSessions + ".csv";
      else
        this.txtLogFile.Text = "";
      try
      {
        this.logTimer.Stop();
        this.stopTimer();
        this.lblLogTime.Text = "Log Time: " + this.getLogTimeSeconds(false) + "sec";
        this.logTime = 0L;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
      this.logger.clearReqs();
      this.changeButtonState(true);
      this.cmdStopLogging.Enabled = false;
      this.cmdStartLogging.Enabled = true;
    }

    private void comboBox_xmlparams_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (this.eVars == null)
        return;
      string varVal = this.comboBox_xmlparams.SelectedValue.ToString();
      List<ECUVariable> list = this.eVars.Where<ECUVariable>((Func<ECUVariable, bool>) (x => x.name.Contains(varVal))).ToList<ECUVariable>();
      if (list.Count <= 0)
        return;
      this.label_custom.Text = list[0].name + " (" + list[0].units + ")";
    }

    private void licenseManagerToolStripMenuItem_Click_1(object sender, EventArgs e)
    {
    }

    private string getECUParams()
    {
      this.regedit.BaseRegistryKey = Registry.CurrentUser;
      this.regedit.SubKey = this.APP_KEY;
      return this.regedit.Read("ECUParams");
    }

    private bool getReadLatch()
    {
      this.regedit.BaseRegistryKey = Registry.CurrentUser;
      this.regedit.SubKey = this.APP_KEY;
      string str = this.regedit.Read("ReadLatch");
      try
      {
        return bool.Parse(str);
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    private bool setReadLatch(bool readOut)
    {
      this.regedit.BaseRegistryKey = Registry.CurrentUser;
      this.regedit.SubKey = this.APP_KEY;
      return this.regedit.Write("ReadLatch", (object) readOut.ToString());
    }

    private bool setECUParams(string ecuparams)
    {
      this.regedit.BaseRegistryKey = Registry.CurrentUser;
      this.regedit.SubKey = this.APP_KEY;
      return this.regedit.Write("ECUParams", (object) ecuparams);
    }

    private void txtLogFile_TextChanged(object sender, EventArgs e)
    {
      if (this.txtLogFile.Text.Equals(""))
        this.cmdStartLogging.Enabled = false;
      else
        this.cmdStartLogging.Enabled = true;
    }

    private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
    {
      int num = (int) MessageBox.Show("CAN Toolbox and Flashing Suite, by John Currie (c) 2016 " + Application.CompanyName, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }

    private bool BitBashConnect()
    {
      this.dice.setHSBaud(BaudRate.CAN_500000);
      this.dice.setJ2534Device(ToolComm.getInstalledDevices()[this.comboBox_J2534_devices.SelectedIndex]);
      if (!this.dice.connect())
        return false;
      uint msgid1 = 0;
      uint msgid2 = 0;
      this.dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid1, 1000U, CANChannel.HS);
      Thread.Sleep(1200);
      bool flag = this.dice.sendMsgCheckDiagResponse(VolvoECUCommands.msgCANReadECMSerial, CANChannel.HS, (byte) 249);
      if (!flag)
      {
        this.dice.stopPeriodicMsg(msgid1, CANChannel.HS);
        this.dice.disconnect();
        this.dice.setHSBaud(BaudRate.CAN_250000);
        if (!this.dice.connect())
          return false;
        Thread.Sleep(2200);
        this.dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid1, 1000U, CANChannel.HS);
        this.dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid2, 1000U, CANChannel.MS);
        Thread.Sleep(1200);
        flag = this.dice.sendMsgCheckDiagResponse(VolvoECUCommands.msgCANReadECMSerial, CANChannel.HS, (byte) 249);
      }
      this.dice.stopPeriodicMsg(msgid1, CANChannel.HS);
      if (this.dice.getHSBaud() == BaudRate.CAN_250000)
        this.dice.stopPeriodicMsg(msgid2, CANChannel.MS);
      return flag;
    }

    private void but_connectdice_Click(object sender, EventArgs e)
    {
      bool flag = this.BitBashConnect();
      switch (this.dice.getHSBaud())
      {
        case BaudRate.CAN_250000:
          this.progressFlash.Maximum = 114;
          break;
        case BaudRate.CAN_500000:
          this.progressFlash.Maximum = 81;
          break;
      }
      if (flag)
      {
        this.but_connectdice.Enabled = false;
        this.cmdReset.Enabled = true;
        this.comboBox_J2534_devices.Enabled = false;
        this.cmdDetectDevices.Enabled = false;
        this.changeButtonState(true);
      }
      else
      {
        int num = (int) MessageBox.Show("DiCE is not connected", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
        try
        {
          this.dice.disconnect();
        }
        catch (Exception ex)
        {
        }
        this.dice = new ToolComm();
      }
    }

    private void but_chooseparams_Click(object sender, EventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = "Volvo XML Parameter Files (*.xml)|*.xml";
      if (openFileDialog.ShowDialog() != DialogResult.OK)
        return;
      this.txtParamsFile.Text = openFileDialog.FileName;
      this.setECUParams(openFileDialog.FileName);
      this.parseParameters();
      if (this.eVars == null)
        return;
      this.SetComboXML();
    }

    private void parseParameters()
    {
      if (this.txtParamsFile.Text.Equals(""))
      {
        int num1 = (int) MessageBox.Show("Please choose a parameters file.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
        try
        {
          this.deserializeXML();
        }
        catch (Exception ex)
        {
          int num2 = (int) MessageBox.Show("Error in parameters file.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
      }
    }

    private void SetComboXML()
    {
      try
      {
        this.comboBox_xmlparams.DisplayMember = "var_txt";
        this.comboBox_xmlparams.ValueMember = "var";
        this.comboBox_xmlparams.DataSource = (object) this.eParams.ecuVars.Select(x => new
        {
          var_txt = x.name,
          var = x.name
        }).ToList();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }

    private void deserializeXML()
    {
      if (this.eVars == null)
      {
        this.txtParamsFile.Text = "";
        this.setECUParams("");
        int num = (int) MessageBox.Show("Error parsing parameters file!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      try
      {
        this.eParams = XmlSerializer.ReadObject(this.txtParamsFile.Text);
        this.eVars = this.eParams.ecuVars;
      }
      catch (Exception ex)
      {
      }
    }

    private void cmdChooseLog_Click(object sender, EventArgs e)
    {
      SaveFileDialog saveFileDialog = new SaveFileDialog();
      saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
      if (saveFileDialog.ShowDialog() == DialogResult.OK)
        this.txtLogFile.Text = saveFileDialog.FileName.ToString();
      else
        this.txtLogFile.Text = "";
    }

    private void cmdStartLogging_Click(object sender, EventArgs e)
    {
      try
      {
        if (this.txtLogFile.Text.Equals(""))
        {
          int num = (int) MessageBox.Show("Please choose a log file.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        else
        {
          this.logger = new ECULogger(this.dice, this.eParams);
          if (!this.p80)
            new DIMComm(this.dice, true).sendMessage("Logging...");
          this.parseParameters();
          this.logger.sendReqs();
          this.logFile = new StreamWriter(this.txtLogFile.Text, false);
          if (this.eParams.displayTime)
            this.logFile.Write("Time (sec),");
          foreach (ECUVariable eVar in (List<ECUVariable>) this.eVars)
          {
            if (eVar.desc.Equals("") || eVar.units.Equals(""))
              this.logFile.Write(eVar.name + ",");
            else
              this.logFile.Write(eVar.desc + "(" + eVar.units + ") " + eVar.name + ",");
          }
          this.logFile.WriteLine();
          this.logTimer.Start();
          this.startTimer();
          this.changeButtonState(false);
          this.cmdStartLogging.Enabled = false;
          this.cmdStopLogging.Enabled = true;
          ++this.numLogSessions;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }

    private void logTimer_Tick_1(object sender, EventArgs e)
    {
      Thread.CurrentThread.CurrentCulture = this.culture;
      Thread.CurrentThread.CurrentUICulture = this.culture;
      string result = "";
      if (!this.logger.requestRecords(ref result))
        return;
      this.processReqs(result);
      if (this.eParams.displayTime)
        this.logFile.Write(this.getLogTimeSeconds(true) + ",");
      foreach (ECUVariable eVar in (List<ECUVariable>) this.eVars)
      {
        int num = (int) eVar.value;
        if (eVar.signed)
          num = !eVar.word ? (int) (sbyte) eVar.value : (int) (short) eVar.value;
        double dbValue = (double) num * eVar.factor + eVar.offset;
        eVar.result = this.logger.getDoublePrecision(dbValue, eVar.precision);
        this.logFile.Write(eVar.result + ",");
      }
      this.logFile.WriteLine();
      this.lblLogTime.Text = "Log Time: " + this.getLogTimeSeconds(false) + "sec";
      if (!this.chkshowvitals.Checked)
        return;
      List<ECUVariable> list1 = this.eVars.Where<ECUVariable>((Func<ECUVariable, bool>) (x => x.name.Contains("pvdkds_w"))).ToList<ECUVariable>();
      if (list1.Count > 0)
      {
        if (this.chkPSI.Checked)
        {
          double num = (double.Parse(list1[0].result) - 1000.0) / 68.9475729;
          this.vitals_boost.Text = this.logger.getDoublePrecision(num > 0.0 ? num : 0.0, list1[0].precision);
        }
        else
          this.vitals_boost.Text = list1[0].result.ToString();
      }
      this.vitals_lambda.Text = this.eVars.Where<ECUVariable>((Func<ECUVariable, bool>) (x => x.name.Contains("lamsoni_w"))).ToList<ECUVariable>()[0].result.ToString();
      List<ECUVariable> list2 = this.eVars.Where<ECUVariable>((Func<ECUVariable, bool>) (x => x.name.Contains("wkrm"))).ToList<ECUVariable>();
      if (list2.Count > 0)
        this.vitals_retard.Text = list2[0].result.ToString();
     List<ECUVariable> list4 = this.eVars.Where<ECUVariable>((Func<ECUVariable, bool>)(x => x.name.Contains("pistnd_w"))).ToList<ECUVariable>();
            if (list2.Count > 0)
                this.vitals_fuelpressure.Text = list4[0].result.ToString();
            List<ECUVariable> list3 = this.eVars.Where<ECUVariable>((Func<ECUVariable, bool>) (x => x.name.Contains(this.comboBox_xmlparams.SelectedValue.ToString()))).ToList<ECUVariable>();
      if (list3.Count <= 0)
        return;
      this.vitals_custom.Text = list3[0].result.ToString();

    }

    private void startTimer()
    {
      this.StateObj = new frmMain.StateObjClass();
      this.StateObj.TimerCanceled = false;
      this.StateObj.lf = this.logFile;
      this.StateObj.TimerReference = new System.Threading.Timer(new TimerCallback(this.TimerTask), (object) this.StateObj, 0, 100);
    }

    private void stopTimer()
    {
      try
      {
        this.StateObj.TimerCanceled = true;
      }
      catch (Exception ex)
      {
        ex.ToString();
      }
    }

    private void TimerTask(object StateObj)
    {
      frmMain.StateObjClass stateObjClass = (frmMain.StateObjClass) StateObj;
      Interlocked.Increment(ref this.logTime);
      if (!stateObjClass.TimerCanceled)
        return;
      stateObjClass.TimerReference.Dispose();
    }

    private string getLogTimeSeconds(bool withMillis)
    {
      if (withMillis)
        return ((double) this.logTime / 10.0).ToString("0.000");
      return (this.logTime / 10L).ToString();
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (!this.dice.DeviceOpen)
        return;
      this.dice.disconnectFinal();
    }

    private List<CANPacket> createSBLList()
    {
      List<CANPacket> canPacketList = new List<CANPacket>();
      int num1 = 0;
      while (num1 < VolvoECUCommands.sbl.Length)
      {
        CANPacket canPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
        byte[] mData = new byte[6];
        int num2 = VolvoECUCommands.sbl.Length - num1;
        for (int index = 0; index < 6; ++index)
          mData[index] = num2 < index + 1 ? (byte) 0 : VolvoECUCommands.sbl[num1 + index];
        canPacket.setMsgData(mData);
        canPacketList.Add(canPacket);
        num1 += 6;
      }
      return canPacketList;
    }

    private void changeButtonState(bool setEnabled)
    {
      this.cmdFlash.Enabled = setEnabled;
      this.cmdRead.Enabled = setEnabled;
      this.cmdReset.Enabled = setEnabled;
      this.cmdStartLogging.Enabled = setEnabled;
      this.cmdStopLogging.Enabled = setEnabled;
      this.resetServiceReminderToolStripMenuItem.Enabled = setEnabled;
    }

    private List<CANPacket> createData8kList()
    {
      List<CANPacket> canPacketList = new List<CANPacket>();
      int num1 = 32768;
      while (num1 < 57344)
      {
        CANPacket canPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
        byte[] mData = new byte[6];
        int num2 = this.binFile.Length - num1;
        for (int index = 0; index < 6; ++index)
          mData[index] = num2 < index + 1 ? (byte) 0 : this.binFile[num1 + index];
        canPacket.setMsgData(mData);
        canPacketList.Add(canPacket);
        num1 += 6;
      }
      return canPacketList;
    }

    private List<CANPacket> createData10kList()
    {
      List<CANPacket> canPacketList = new List<CANPacket>();
      int num1 = 65536;
      while (num1 < 1048576)
      {
        CANPacket canPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
        byte[] mData = new byte[6];
        int num2 = this.binFile.Length - num1;
        for (int index = 0; index < 6; ++index)
          mData[index] = num2 < index + 1 ? (byte) 0 : this.binFile[num1 + index];
        canPacket.setMsgData(mData);
        canPacketList.Add(canPacket);
        num1 += 6;
      }
      return canPacketList;
    }

    public int getHexValueFromString(string input)
    {
      if (input.Contains<char>('x'))
        input = input.Split('x')[1];
      if (input.Length == 2)
        return (int) this.getAddressFromString(input)[0];
      if (input.Length == 4)
      {
        byte[] addressFromString = this.getAddressFromString(input);
        return (int) addressFromString[1] + (int) (ushort) ((uint) addressFromString[0] * 256U);
      }
      if (input.Length != 8)
        throw new Exception();
      byte[] addressFromString1 = this.getAddressFromString(input);
      return (int) addressFromString1[3] + (int) addressFromString1[2] * 256 + (int) addressFromString1[1] * 65536 + (int) addressFromString1[0] * 16777216;
    }

    private byte[] getAddressFromString(string input)
    {
      if (input.Contains<char>('x'))
        input = input.Split('x')[1];
      byte[] numArray = new byte[input.Length / 2];
      char[] charArray = input.ToCharArray();
      int index = 0;
      while (index < charArray.Length)
      {
        numArray[index / 2] = (byte) (16U * (uint) this.getByteFromChar(charArray[index]));
        numArray[index / 2] += this.getByteFromChar(charArray[index + 1]);
        index += 2;
      }
      return numArray;
    }

    private byte getByteFromChar(char c)
    {
      switch (c)
      {
        case '0':
          return 0;
        case '1':
          return 1;
        case '2':
          return 2;
        case '3':
          return 3;
        case '4':
          return 4;
        case '5':
          return 5;
        case '6':
          return 6;
        case '7':
          return 7;
        case '8':
          return 8;
        case '9':
          return 9;
        case 'A':
        case 'a':
          return 10;
        case 'B':
        case 'b':
          return 11;
        case 'C':
        case 'c':
          return 12;
        case 'D':
        case 'd':
          return 13;
        case 'E':
        case 'e':
          return 14;
        case 'F':
        case 'f':
          return 15;
        default:
          return 0;
      }
    }

    private void licenseManagerToolStripMenuItem_Click(object sender, EventArgs e)
    {
    }

    private void cmdReset_Click(object sender, EventArgs e)
    {
      if (this.eProg != null)
        return;
      this.eProg = new ECUProgrammer(this.dice, (byte[]) null, (byte[]) null, false, this.p80);
      this.eProg.sendReset();
      if (!this.p80)
        new DIMComm(this.dice, false).setTime();
      this.eProg = (ECUProgrammer) null;
    }

    private void cmdRead_Click(object sender, EventArgs e)
    {
      this.saveBINFile();
      if (this.txtBin.Text == "")
      {
        int num1 = (int) MessageBox.Show("Please choose a BIN file.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else if (!this.dice.DeviceOpen)
      {
        int num2 = (int) MessageBox.Show("Please connect the DiCE first.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else if (MessageBox.Show("Is the key in pos II with the engine NOT running?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
      {
        this.txtBin.Text = "";
      }
      else
      {
        this.flashTime = 0;
        this.changeButtonState(false);
        this.eProg = new ECUProgrammer(this.dice, VolvoECUCommands.sbl, (byte[]) null, this.flash512, this.p80);
        new Thread((ThreadStart) (() =>
        {
          this.eProg.sendModuleReset();
          this.eProg.sendSilence();
          this.eProg.startPBL();
          this.eProg.startSBL(VolvoECUCommands.sbl, true);
          this.readout = this.eProg.readECU(false);
          this.ramReadout = this.eProg.readECU(true);
          this.eProg.sendReset();
          Thread.Sleep(2000);
          if (this.p80)
            return;
          new DIMComm(this.dice, false).setTime();
        })).Start();
        this.progressFlash.Maximum = 861;
        this.readTimer.Start();
      }
    }

    private void readTimer_Tick(object sender, EventArgs e)
    {
      this.lblTime.Text = "Read Time: " + this.flashTime.ToString();
      lock (new object())
      {
        this.progressFlash.Value = this.flashTime > this.progressFlash.Maximum ? this.progressFlash.Maximum : this.flashTime;
        if (this.eProg.doneFlashing)
        {
          this.readTimer.Stop();
          this.progressFlash.Value = this.progressFlash.Maximum;
          string str = this.txtBin.Text;
          int length1 = str.LastIndexOf(".");
          if (length1 >= 0)
            str = str.Substring(0, length1);
          if (this.ramReadout != null)
            File.WriteAllBytes(str + "_RAM.bin", this.ramReadout);
          if (this.readout == null)
          {
            int num = (int) MessageBox.Show("Error reading file!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            this.changeButtonState(true);
            return;
          }
          if (this.readout.Length == 4)
          {
            if (ECUProgrammer.checkArrayEq(this.readout, new byte[4]
            {
              (byte) 68,
              (byte) 69,
              (byte) 78,
              (byte) 89
            }))
            {
              int num = (int) MessageBox.Show("This file cannot be read! An attempt to read and save the RAM was made.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
              this.changeButtonState(true);
              return;
            }
          }
          //VolvoChecksumUpdater volvoChecksumUpdater = new VolvoChecksumUpdater(this.readout);
          int length2 = this.readout.Length;
          if (this.readout[length2 - 1] != (byte) 131 || this.readout[length2 - 2] != (byte) 131) //|| !volvoChecksumUpdater.updateChecksums(true))
          {
            int num = (int) MessageBox.Show("This file is corrupt. Please contact the distributor.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            this.changeButtonState(true);
            this.txtBin.Text = "";
            return;
          }
          File.WriteAllBytes(this.txtBin.Text, this.readout);
          this.changeButtonState(true);
          this.setReadLatch(true);
          this.lblTime.Text = "Done Reading!";
          this.progressFlash.Value = 0;
        }
      }
      ++this.flashTime;
    }

    private void resetServiceReminderToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (!this.dice.DeviceOpen)
      {
        int num1 = (int) MessageBox.Show("Please connect the DiCE first.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
      else
      {
        if (MessageBox.Show("Would you like to reset the SRI?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes || MessageBox.Show("Is the key in pos II with the engine NOT running?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
          return;
        this.eProg = new ECUProgrammer(this.dice, (byte[]) null, (byte[]) null, this.flash512, this.p80);
        if (!new DIMComm(this.dice, false).resetSRI())
        {
          int num2 = (int) MessageBox.Show("Failed to reset the service indicator!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        else
        {
          int num3 = (int) MessageBox.Show("Service indicator reset!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
            this.components = new System.ComponentModel.Container();
            this.flashTimer = new System.Windows.Forms.Timer(this.components);
            this.but_connectdice = new System.Windows.Forms.Button();
            this.comboBox_J2534_devices = new System.Windows.Forms.ComboBox();
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetServiceReminderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.readTimer = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.progressFlash = new System.Windows.Forms.ProgressBar();
            this.cmdRead = new System.Windows.Forms.Button();
            this.cmdFlash = new System.Windows.Forms.Button();
            this.cmdReset = new System.Windows.Forms.Button();
            this.txtBin = new System.Windows.Forms.TextBox();
            this.lblTime = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.chkPSI = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.vitals_custom = new System.Windows.Forms.Label();
            this.label_custom = new System.Windows.Forms.Label();
            this.vitals_retard = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.vitals_fuelpressure = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.vitals_lambda = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.vitals_boost = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_xmlparams = new System.Windows.Forms.ComboBox();
            this.lblLogTime = new System.Windows.Forms.Label();
            this.chkshowvitals = new System.Windows.Forms.CheckBox();
            this.cmdStopLogging = new System.Windows.Forms.Button();
            this.cmdStartLogging = new System.Windows.Forms.Button();
            this.cmdChooseLog = new System.Windows.Forms.Button();
            this.txtParamsFile = new System.Windows.Forms.TextBox();
            this.txtLogFile = new System.Windows.Forms.TextBox();
            this.but_chooseparams = new System.Windows.Forms.Button();
            this.logTimer = new System.Windows.Forms.Timer(this.components);
            this.cmdDetectDevices = new System.Windows.Forms.Button();
            this.menuMain.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flashTimer
            // 
            this.flashTimer.Interval = 1000;
            this.flashTimer.Tick += new System.EventHandler(this.flashTimer_Tick);
            // 
            // but_connectdice
            // 
            this.but_connectdice.FlatAppearance.BorderColor = System.Drawing.Color.Blue;
            this.but_connectdice.Font = new System.Drawing.Font("MS Reference Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.but_connectdice.Location = new System.Drawing.Point(479, 5);
            this.but_connectdice.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.but_connectdice.Name = "but_connectdice";
            this.but_connectdice.Size = new System.Drawing.Size(94, 21);
            this.but_connectdice.TabIndex = 3;
            this.but_connectdice.Text = "Connect";
            this.but_connectdice.UseVisualStyleBackColor = true;
            this.but_connectdice.Click += new System.EventHandler(this.but_connectdice_Click);
            // 
            // comboBox_J2534_devices
            // 
            this.comboBox_J2534_devices.Font = new System.Drawing.Font("MS Reference Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox_J2534_devices.FormattingEnabled = true;
            this.comboBox_J2534_devices.Location = new System.Drawing.Point(4, 6);
            this.comboBox_J2534_devices.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.comboBox_J2534_devices.Name = "comboBox_J2534_devices";
            this.comboBox_J2534_devices.Size = new System.Drawing.Size(357, 21);
            this.comboBox_J2534_devices.TabIndex = 1;
            // 
            // menuMain
            // 
            this.menuMain.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Padding = new System.Windows.Forms.Padding(9, 2, 0, 2);
            this.menuMain.Size = new System.Drawing.Size(1011, 24);
            this.menuMain.TabIndex = 9;
            this.menuMain.Text = "menuStrip1";
            this.menuMain.Visible = false;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetServiceReminderToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // resetServiceReminderToolStripMenuItem
            // 
            this.resetServiceReminderToolStripMenuItem.Enabled = false;
            this.resetServiceReminderToolStripMenuItem.Name = "resetServiceReminderToolStripMenuItem";
            this.resetServiceReminderToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            this.resetServiceReminderToolStripMenuItem.Text = "Reset Service Reminder...";
            this.resetServiceReminderToolStripMenuItem.Click += new System.EventHandler(this.resetServiceReminderToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // readTimer
            // 
            this.readTimer.Interval = 1000;
            this.readTimer.Tick += new System.EventHandler(this.readTimer_Tick);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tabControl1.Font = new System.Drawing.Font("MS Reference Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.Location = new System.Drawing.Point(0, 39);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1011, 342);
            this.tabControl1.TabIndex = 15;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.progressFlash);
            this.tabPage1.Controls.Add(this.cmdRead);
            this.tabPage1.Controls.Add(this.cmdFlash);
            this.tabPage1.Controls.Add(this.cmdReset);
            this.tabPage1.Controls.Add(this.txtBin);
            this.tabPage1.Controls.Add(this.lblTime);
            this.tabPage1.Font = new System.Drawing.Font("MS Reference Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.tabPage1.Size = new System.Drawing.Size(1003, 316);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Flashing";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("MS Reference Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(910, 293);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 15);
            this.label2.TabIndex = 16;
            this.label2.Text = "RIP Dream3R";
            this.label2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.label2_MouseDown);
            // 
            // progressFlash
            // 
            this.progressFlash.Location = new System.Drawing.Point(10, 64);
            this.progressFlash.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.progressFlash.Maximum = 168907;
            this.progressFlash.Name = "progressFlash";
            this.progressFlash.Size = new System.Drawing.Size(983, 16);
            this.progressFlash.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressFlash.TabIndex = 4;
            // 
            // cmdRead
            // 
            this.cmdRead.Enabled = false;
            this.cmdRead.Location = new System.Drawing.Point(794, 36);
            this.cmdRead.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.cmdRead.Name = "cmdRead";
            this.cmdRead.Size = new System.Drawing.Size(199, 20);
            this.cmdRead.TabIndex = 13;
            this.cmdRead.Text = "Read ECU";
            this.cmdRead.UseVisualStyleBackColor = true;
            this.cmdRead.Click += new System.EventHandler(this.cmdRead_Click);
            // 
            // cmdFlash
            // 
            this.cmdFlash.Enabled = false;
            this.cmdFlash.Location = new System.Drawing.Point(794, 8);
            this.cmdFlash.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.cmdFlash.Name = "cmdFlash";
            this.cmdFlash.Size = new System.Drawing.Size(199, 20);
            this.cmdFlash.TabIndex = 8;
            this.cmdFlash.Text = "Flash ECU";
            this.cmdFlash.UseVisualStyleBackColor = true;
            this.cmdFlash.Click += new System.EventHandler(this.cmdFlash_Click);
            // 
            // cmdReset
            // 
            this.cmdReset.Enabled = false;
            this.cmdReset.Location = new System.Drawing.Point(756, 88);
            this.cmdReset.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.cmdReset.Name = "cmdReset";
            this.cmdReset.Size = new System.Drawing.Size(237, 39);
            this.cmdReset.TabIndex = 11;
            this.cmdReset.Text = "Emergency Reset";
            this.cmdReset.UseVisualStyleBackColor = true;
            this.cmdReset.Click += new System.EventHandler(this.cmdReset_Click);
            // 
            // txtBin
            // 
            this.txtBin.Location = new System.Drawing.Point(10, 8);
            this.txtBin.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtBin.Name = "txtBin";
            this.txtBin.ReadOnly = true;
            this.txtBin.Size = new System.Drawing.Size(774, 20);
            this.txtBin.TabIndex = 6;
            // 
            // lblTime
            // 
            this.lblTime.AutoSize = true;
            this.lblTime.Location = new System.Drawing.Point(7, 41);
            this.lblTime.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(77, 15);
            this.lblTime.TabIndex = 5;
            this.lblTime.Text = "Flash Time: ";
            this.lblTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.chkPSI);
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Controls.Add(this.comboBox_xmlparams);
            this.tabPage2.Controls.Add(this.lblLogTime);
            this.tabPage2.Controls.Add(this.chkshowvitals);
            this.tabPage2.Controls.Add(this.cmdStopLogging);
            this.tabPage2.Controls.Add(this.cmdStartLogging);
            this.tabPage2.Controls.Add(this.cmdChooseLog);
            this.tabPage2.Controls.Add(this.txtParamsFile);
            this.tabPage2.Controls.Add(this.txtLogFile);
            this.tabPage2.Controls.Add(this.but_chooseparams);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.tabPage2.Size = new System.Drawing.Size(1003, 316);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Logging";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // chkPSI
            // 
            this.chkPSI.AutoSize = true;
            this.chkPSI.Location = new System.Drawing.Point(11, 100);
            this.chkPSI.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.chkPSI.Name = "chkPSI";
            this.chkPSI.Size = new System.Drawing.Size(82, 19);
            this.chkPSI.TabIndex = 51;
            this.chkPSI.Text = "Boost PSI";
            this.chkPSI.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.vitals_custom);
            this.groupBox1.Controls.Add(this.label_custom);
            this.groupBox1.Controls.Add(this.vitals_retard);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.vitals_fuelpressure);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.vitals_lambda);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.vitals_boost);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(11, 129);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.groupBox1.Size = new System.Drawing.Size(976, 127);
            this.groupBox1.TabIndex = 50;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Vitals";
            // 
            // vitals_custom
            // 
            this.vitals_custom.AutoSize = true;
            this.vitals_custom.Location = new System.Drawing.Point(870, 71);
            this.vitals_custom.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.vitals_custom.Name = "vitals_custom";
            this.vitals_custom.Size = new System.Drawing.Size(12, 15);
            this.vitals_custom.TabIndex = 7;
            this.vitals_custom.Text = "-";
            // 
            // label_custom
            // 
            this.label_custom.AutoSize = true;
            this.label_custom.Location = new System.Drawing.Point(823, 25);
            this.label_custom.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label_custom.Name = "label_custom";
            this.label_custom.Size = new System.Drawing.Size(62, 15);
            this.label_custom.TabIndex = 6;
            this.label_custom.Text = "Pls Select";
            // 
            // vitals_retard
            // 
            this.vitals_retard.AutoSize = true;
            this.vitals_retard.Location = new System.Drawing.Point(517, 72);
            this.vitals_retard.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.vitals_retard.Name = "vitals_retard";
            this.vitals_retard.Size = new System.Drawing.Size(12, 15);
            this.vitals_retard.TabIndex = 5;
            this.vitals_retard.Text = "-";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(-3, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 23);
            this.label6.TabIndex = 8;
            // 
            // vitals_fuelpressure
            // 
            this.vitals_fuelpressure.Location = new System.Drawing.Point(700, 72);
            this.vitals_fuelpressure.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.vitals_fuelpressure.Name = "vitals_fuelpressure";
            this.vitals_fuelpressure.Size = new System.Drawing.Size(22, 26);
            this.vitals_fuelpressure.TabIndex = 6;
            this.vitals_fuelpressure.Text = "-";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(517, 25);
            this.label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(68, 15);
            this.label5.TabIndex = 4;
            this.label5.Text = "Ign Retard";
            // 
            // vitals_lambda
            // 
            this.vitals_lambda.AutoSize = true;
            this.vitals_lambda.Location = new System.Drawing.Point(274, 71);
            this.vitals_lambda.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.vitals_lambda.Name = "vitals_lambda";
            this.vitals_lambda.Size = new System.Drawing.Size(12, 15);
            this.vitals_lambda.TabIndex = 3;
            this.vitals_lambda.Text = "-";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(274, 22);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "Lambda";
            // 
            // vitals_boost
            // 
            this.vitals_boost.AutoSize = true;
            this.vitals_boost.Location = new System.Drawing.Point(34, 71);
            this.vitals_boost.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.vitals_boost.Name = "vitals_boost";
            this.vitals_boost.Size = new System.Drawing.Size(12, 15);
            this.vitals_boost.TabIndex = 1;
            this.vitals_boost.Text = "-";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 22);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Boost";
            // 
            // comboBox_xmlparams
            // 
            this.comboBox_xmlparams.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_xmlparams.FormattingEnabled = true;
            this.comboBox_xmlparams.Location = new System.Drawing.Point(11, 9);
            this.comboBox_xmlparams.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.comboBox_xmlparams.Name = "comboBox_xmlparams";
            this.comboBox_xmlparams.Size = new System.Drawing.Size(346, 21);
            this.comboBox_xmlparams.TabIndex = 49;
            this.comboBox_xmlparams.SelectedIndexChanged += new System.EventHandler(this.comboBox_xmlparams_SelectedIndexChanged);
            // 
            // lblLogTime
            // 
            this.lblLogTime.AutoSize = true;
            this.lblLogTime.Location = new System.Drawing.Point(443, 290);
            this.lblLogTime.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblLogTime.Name = "lblLogTime";
            this.lblLogTime.Size = new System.Drawing.Size(64, 15);
            this.lblLogTime.TabIndex = 48;
            this.lblLogTime.Text = "Log Time:";
            this.lblLogTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // chkshowvitals
            // 
            this.chkshowvitals.AutoSize = true;
            this.chkshowvitals.Location = new System.Drawing.Point(174, 100);
            this.chkshowvitals.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.chkshowvitals.Name = "chkshowvitals";
            this.chkshowvitals.Size = new System.Drawing.Size(92, 19);
            this.chkshowvitals.TabIndex = 47;
            this.chkshowvitals.Text = "Show Vitals";
            this.chkshowvitals.UseVisualStyleBackColor = true;
            // 
            // cmdStopLogging
            // 
            this.cmdStopLogging.Enabled = false;
            this.cmdStopLogging.Location = new System.Drawing.Point(219, 266);
            this.cmdStopLogging.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.cmdStopLogging.Name = "cmdStopLogging";
            this.cmdStopLogging.Size = new System.Drawing.Size(196, 39);
            this.cmdStopLogging.TabIndex = 46;
            this.cmdStopLogging.Text = "Stop Logging";
            this.cmdStopLogging.UseVisualStyleBackColor = true;
            this.cmdStopLogging.Click += new System.EventHandler(this.cmdStopLogging_Click);
            // 
            // cmdStartLogging
            // 
            this.cmdStartLogging.Enabled = false;
            this.cmdStartLogging.Location = new System.Drawing.Point(11, 266);
            this.cmdStartLogging.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.cmdStartLogging.Name = "cmdStartLogging";
            this.cmdStartLogging.Size = new System.Drawing.Size(196, 39);
            this.cmdStartLogging.TabIndex = 45;
            this.cmdStartLogging.Text = "Start Logging";
            this.cmdStartLogging.UseVisualStyleBackColor = true;
            this.cmdStartLogging.Click += new System.EventHandler(this.cmdStartLogging_Click);
            // 
            // cmdChooseLog
            // 
            this.cmdChooseLog.Location = new System.Drawing.Point(431, 69);
            this.cmdChooseLog.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.cmdChooseLog.Name = "cmdChooseLog";
            this.cmdChooseLog.Size = new System.Drawing.Size(138, 21);
            this.cmdChooseLog.TabIndex = 44;
            this.cmdChooseLog.Text = "Log Save Location";
            this.cmdChooseLog.UseVisualStyleBackColor = true;
            this.cmdChooseLog.Click += new System.EventHandler(this.cmdChooseLog_Click);
            // 
            // txtParamsFile
            // 
            this.txtParamsFile.Location = new System.Drawing.Point(11, 40);
            this.txtParamsFile.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.txtParamsFile.Name = "txtParamsFile";
            this.txtParamsFile.ReadOnly = true;
            this.txtParamsFile.Size = new System.Drawing.Size(408, 20);
            this.txtParamsFile.TabIndex = 43;
            // 
            // txtLogFile
            // 
            this.txtLogFile.Location = new System.Drawing.Point(11, 70);
            this.txtLogFile.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.txtLogFile.Name = "txtLogFile";
            this.txtLogFile.ReadOnly = true;
            this.txtLogFile.Size = new System.Drawing.Size(408, 20);
            this.txtLogFile.TabIndex = 42;
            // 
            // but_chooseparams
            // 
            this.but_chooseparams.Location = new System.Drawing.Point(431, 40);
            this.but_chooseparams.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.but_chooseparams.Name = "but_chooseparams";
            this.but_chooseparams.Size = new System.Drawing.Size(138, 20);
            this.but_chooseparams.TabIndex = 41;
            this.but_chooseparams.Text = "Load Params";
            this.but_chooseparams.UseVisualStyleBackColor = true;
            this.but_chooseparams.Click += new System.EventHandler(this.but_chooseparams_Click);
            // 
            // logTimer
            // 
            this.logTimer.Interval = 1;
            this.logTimer.Tick += new System.EventHandler(this.logTimer_Tick_1);
            // 
            // cmdDetectDevices
            // 
            this.cmdDetectDevices.Font = new System.Drawing.Font("MS Reference Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdDetectDevices.Location = new System.Drawing.Point(373, 5);
            this.cmdDetectDevices.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.cmdDetectDevices.Name = "cmdDetectDevices";
            this.cmdDetectDevices.Size = new System.Drawing.Size(94, 21);
            this.cmdDetectDevices.TabIndex = 0;
            this.cmdDetectDevices.Text = "Refresh";
            this.cmdDetectDevices.UseVisualStyleBackColor = true;
            this.cmdDetectDevices.Click += new System.EventHandler(this.cmdDetectDevices_Click);
            // 
            // frmMain
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(1011, 381);
            this.Controls.Add(this.but_connectdice);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.comboBox_J2534_devices);
            this.Controls.Add(this.cmdDetectDevices);
            this.Controls.Add(this.menuMain);
            this.Font = new System.Drawing.Font("MS Outlook", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MainMenuStrip = this.menuMain;
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Open Moose";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    private class StateObjClass
    {
      public StreamWriter lf;
      public System.Threading.Timer TimerReference;
      public bool TimerCanceled;
    }

    private void label2_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
    {
    if (e.Button == MouseButtons.Left)
      if (Control.ModifierKeys == Keys.Shift)
            {
               {
               string nl = System.Environment.NewLine;
                        int num1 = (int)MessageBox.Show(Application.ProductName + " was developed by the Volvo enthusiast community, for the Volvo enthusiast community." + nl + nl + "This application includes code originally developed by Dream3R, which was greedily stolen and profited from, then stolen again and given back to the Volvo enthusiast community." + nl + nl + "Special thanks to the ME7 enthusiasts at NefariousMotorSports including Dream3R (RIP) for discovering how to flash over DiCE, s60rawr for securing this version of the code, rlinewiz for debugging and programming, and the patrons in the Volvo ME7 Thread for testing and feedback." + nl + nl + "This project is open-source and free to use and modify, so long as you give credit where credit is due."+nl+nl+"FREE THE MOOSE!", Application.ProductName, MessageBoxButtons.OK);
                    }
                }
     }
   }
}
