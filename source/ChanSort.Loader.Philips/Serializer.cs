using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using ChanSort.Api;

namespace ChanSort.Loader.Philips
{
  class Serializer : SerializerBase
  {
    private readonly ChannelList dvbcChannels = new ChannelList(SignalSource.DvbC, "DVB-C");
    private readonly ChannelList satChannels = new ChannelList(SignalSource.DvbS, "DVB-S");
    private readonly ChannelList dvbtChannels = new ChannelList(SignalSource.DvbT, "DVB-T");

    private readonly List<FileData> fileDataList = new List<FileData>();

    [DllImport("Air.dll")]
    public static extern int ConvertToXML_Air([MarshalAs(UnmanagedType.LPArray)] byte[] path, [MarshalAs(UnmanagedType.LPArray)] byte[] read_buff);
    [DllImport("Air.dll")]
    public static extern int ConvertToBIN_Air([MarshalAs(UnmanagedType.LPArray)] byte[] read_buff);
    
    [DllImport("Cable.dll")]
    public static extern int ConvertToXML_Cable([MarshalAs(UnmanagedType.LPArray)] byte[] path, [MarshalAs(UnmanagedType.LPArray)] byte[] read_buff);
    [DllImport("Cable.dll")]
    public static extern int ConvertToBIN_Cable([MarshalAs(UnmanagedType.LPArray)] byte[] read_buff);

    [DllImport("dvbs2_cte.dll")]
    public static extern int ConvertToXML_Satellite([MarshalAs(UnmanagedType.LPArray)] byte[] path, [MarshalAs(UnmanagedType.LPArray)] byte[] read_buff);
    [DllImport("dvbs2_cte.dll")]
    public static extern int ConvertToBin_Satellite([MarshalAs(UnmanagedType.LPArray)] byte[] read_buff);

    #region ctor()
    public Serializer(string inputFile) : base(inputFile)
    {
      this.Features.ChannelNameEdit = ChannelNameEditMode.All;
      this.Features.CanSkipChannels = false;
      this.Features.CanLockChannels = true;
      this.Features.CanHideChannels = true;
      this.Features.DeleteMode = DeleteMode.Physically;
      this.Features.CanSaveAs = false;
      this.Features.AllowGapsInFavNumbers = false;

      this.DataRoot.AddChannelList(this.dvbcChannels);
      this.DataRoot.AddChannelList(this.satChannels);
      this.DataRoot.AddChannelList(this.dvbtChannels);

      this.dvbcChannels.VisibleColumnFieldNames.Add("Source");
      this.dvbtChannels.VisibleColumnFieldNames.Add("Source");

      foreach (var list in this.DataRoot.ChannelLists)
      {
        list.VisibleColumnFieldNames.Remove("PcrPid");
        list.VisibleColumnFieldNames.Remove("VideoPid");
        list.VisibleColumnFieldNames.Remove("AudioPid");
        list.VisibleColumnFieldNames.Remove("Skip");
        list.VisibleColumnFieldNames.Remove("ShortName");
        list.VisibleColumnFieldNames.Remove("Provider");
      }
    }
    #endregion

    #region Load()
    public override void Load()
    {
      // support for files in a ChannelList directory structure
      bool isChannelMapFolderStructure = false;
      var dir = Path.GetDirectoryName(this.FileName);
      var dirName = Path.GetFileName(dir).ToLower();
      if (dirName == "channellib" || dirName == "s2channellib")
      {
        dir = Path.GetDirectoryName(dir);
      }

      var binFile = Path.Combine(dir, "chanLst.bin");  // the .bin file is used as a proxy for the whole directory structure
      if (File.Exists(binFile))
      {
        this.FileName = binFile;
        isChannelMapFolderStructure = true;
      }

      if (isChannelMapFolderStructure)
      {
        // Air
        byte[] xmlData = new byte[10485760];
        String channelLibDir = Path.Combine(dir, "channellib").ToString() + "\\";
        try
        {
          int retVal = ConvertToXML_Air(Encoding.ASCII.GetBytes(channelLibDir), xmlData);
          if (retVal == -1)
            throw new FileLoadException("\"" + channelLibDir + "\" does not contain a Philips air channel list");
          if (retVal > 0) {
            switch (retVal)
            {
              case 1:
                throw new FileLoadException("Path \"" + channelLibDir + "\" not found");
              case 2:
                throw new FileLoadException("Channellist in path \"" + channelLibDir + "\" has incorrect file format");
              case 3:
                throw new FileLoadException("Channellist in path \"" + channelLibDir + "\" is incompatible");
              case 4:
                throw new FileLoadException("Channellist in path \"" + channelLibDir + "\" is from an invalid product");
              default:
                throw new FileLoadException("Unknown error when parsing Channellist in path \"" + channelLibDir + "\"");
            }
          }
          int terminator = Array.IndexOf(xmlData, (byte)0);
          this.LoadXmlData("air", Encoding.ASCII.GetString(xmlData, 0, terminator).Trim());
        } catch (DllNotFoundException)
        {
          throw new FileLoadException("DLL file 'Air.dll' from Philips Channel Editor is missing. Please copy this file into the ChanSort directory and try again.");
        }

        // Cable
        xmlData = new byte[10485760];
        try
        {
          int retVal = ConvertToXML_Cable(Encoding.ASCII.GetBytes(channelLibDir), xmlData);
          if (retVal == -1)
            throw new FileLoadException("\"" + channelLibDir + "\" does not contain a Philips cable channel list");
          if (retVal > 0) {
            switch (retVal)
            {
              case 1:
                throw new FileLoadException("Path \"" + channelLibDir + "\" not found");
              case 2:
                throw new FileLoadException("Channellist in path \"" + channelLibDir + "\" has incorrect file format");
              case 3:
                throw new FileLoadException("Channellist in path \"" + channelLibDir + "\" is incompatible");
              case 4:
                throw new FileLoadException("Channellist in path \"" + channelLibDir + "\" is from an invalid product");
              default:
                throw new FileLoadException("Unknown error when parsing Channellist in path \"" + channelLibDir + "\"");
            }
          }
          int terminator = Array.IndexOf(xmlData, (byte)0);
          this.LoadXmlData("cable", Encoding.ASCII.GetString(xmlData, 0, terminator).Trim());
        } catch (DllNotFoundException)
        {
          throw new FileLoadException("DLL file 'Cable.dll' from Philips Channel Editor is missing. Please copy this file into the ChanSort directory and try again.");
        }

        // Satellite
        xmlData = new byte[10485760];
        String s2channelLibDir = Path.Combine(dir, "s2channellib").ToString() + "\\";
        try
        {
          int retVal = ConvertToXML_Satellite(Encoding.ASCII.GetBytes(s2channelLibDir), xmlData);
          if (retVal == -1)
            throw new FileLoadException("\"" + s2channelLibDir + "\" does not contain a Philips satellite channel list");
          if (retVal > 0) {
            switch (retVal)
            {
              case 1:
                throw new FileLoadException("Path \"" + s2channelLibDir + "\" not found");
              case 2:
                throw new FileLoadException("Channellist in path \"" + s2channelLibDir + "\" has incorrect file format");
              case 3:
                throw new FileLoadException("Channellist in path \"" + s2channelLibDir + "\" is incompatible");
              default:
                throw new FileLoadException("Unknown error when parsing Channellist in path \"" + s2channelLibDir + "\"");
            }
          }
          int terminator = Array.IndexOf(xmlData, (byte)0);
          this.LoadXmlData("satellite", Encoding.ASCII.GetString(xmlData, 0, terminator).Trim());
        } catch (DllNotFoundException)
        {
          throw new FileLoadException("DLL file 'dvbs2_cte.dll' from Philips Channel Editor is missing. Please copy this file into the ChanSort directory and try again.");
        }
      }
      else
      {
        throw new FileLoadException("\"" + dir + "\" does not contain a Philips channel list");
      }
    }
    #endregion

    #region LoadXmlData()
    private void LoadXmlData(string fileType, string contents)
    {
      bool fail = false;
      var fileData = new FileData();
      try
      {
        fileData.fileType = fileType;
        fileData.doc = new XmlDocument();
        fileData.textContent = contents;
        fileData.newline = fileData.textContent.Contains("\r\n") ? "\r\n" : "\n";

        var settings = new XmlReaderSettings
        {
          CheckCharacters = false,
          IgnoreProcessingInstructions = true,
          ValidationFlags = XmlSchemaValidationFlags.None,
          DtdProcessing = DtdProcessing.Ignore
        };
        using (var reader = XmlReader.Create(new StringReader(fileData.textContent), settings))
        {
          fileData.doc.Load(reader);
        }
      }
      catch (Exception)
      {
        fail = true;
      }

      var root = fileData.doc.FirstChild;
      if (root is XmlDeclaration)
        root = root.NextSibling;
      if (fail || root == null || root.LocalName != "ChannelMap")
        throw new FileLoadException("Converter for file type \"" + fileType + "\" did not return valid XML");

      fail = true;
      foreach (XmlNode child in root.ChildNodes)
      {
        if (child.Name == "ChannelData")
        {
          root = child;
          fail = false;
          break;
        }
      }
      if (fail)
        throw new FileLoadException("\"" + fileType + "\" is not a supported Philips file");

      int rowId = 0;
      ChannelList curList = null;
      foreach (XmlNode child in root.ChildNodes)
      {
        if (child.LocalName == "Channel")
        {
          if (rowId == 0)
            curList = this.DetectFormatAndFeatures(fileData, child);
          if (curList != null)
            this.ReadChannel(fileData, curList, child, rowId++);
        }
      }
      this.fileDataList.Add(fileData);
    }
    #endregion

    #region DetectFormatAndFeatures()
    private ChannelList DetectFormatAndFeatures(FileData file, XmlNode node)
    {
      this.Features.SupportedFavorites = Favorites.A;
      this.Features.SortedFavorites = true;

      ChannelList chList = null;
      switch (file.fileType)
      {
        case "air":
          chList = this.dvbtChannels;
          break;
        case "cable":
          chList = this.dvbcChannels;
          break;
        case "satellite":
          chList = this.satChannels;
          break;
      }

      return chList;
    }
    #endregion

    #region ReadChannel()
    private void ReadChannel(FileData file, ChannelList curList, XmlNode node, int rowId)
    {
      var data = new Dictionary<string,string>(StringComparer.InvariantCultureIgnoreCase);
      foreach (XmlNode child in node)
        data.Add(child.Name, child.InnerText);

      int uniqueId = rowId;
      var chan = new Channel(curList.SignalSource & SignalSource.MaskAdInput, rowId, uniqueId, node);
      chan.OldProgramNr = -1;
      chan.IsDeleted = false;

      chan.RawSatellite = data.TryGet("SatName");
      chan.Satellite = DecodeName(chan.RawSatellite);
      chan.OldProgramNr = ParseInt(data.TryGet("ChNum"));
      chan.RawName = data.TryGet("ChName");
      chan.Name = DecodeName(chan.RawName);
      chan.Lock = data.TryGet("ChLock") == "true";
      chan.Hidden = data.TryGet("UserHide") == "1";
      bool fav = data.TryGet("Fav") == "true";
      chan.OldFavIndex[0] = fav ? chan.OldProgramNr : -1;
      chan.OriginalNetworkId = ParseInt(data.TryGet("OnId"));
      chan.TransportStreamId = ParseInt(data.TryGet("TsId"));
      chan.ServiceId = ParseInt(data.TryGet("SvcId"));
      decimal.TryParse(data.TryGet("Freq"), NumberStyles.Integer | NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"), out var freq);
      chan.FreqInMhz = freq;
      if (data.TryGetValue("SvcType", out var svc))
        chan.ServiceType = svc == "TV" ? 1 : svc == "RADIO" ? 2 : 0; // 1=SD-TV, 2=Radio
      chan.SignalSource |= LookupData.Instance.IsRadioTvOrData(chan.ServiceType);
      chan.SymbolRate = ParseInt(data.TryGet("SymRate"));
      if (data.TryGetValue("Polarity", out var pol))
        chan.Polarity = pol == "HORIZONTAL" ? 'H' : 'V';
      chan.Hidden |= data.TryGet("SysHide") == "1";

      if ((chan.SignalSource & SignalSource.MaskAdInput) == SignalSource.DvbT)
        chan.ChannelOrTransponder = LookupData.Instance.GetDvbtTransponder(chan.FreqInMhz).ToString();
      else if ((chan.SignalSource & SignalSource.MaskAdInput) == SignalSource.DvbC)
        chan.ChannelOrTransponder = LookupData.Instance.GetDvbcChannelName(chan.FreqInMhz);

      DataRoot.AddChannel(curList, chan);
    }
    #endregion

    #region DecodeName()
    private string DecodeName(string input)
    {
      if (input == null || !input.Trim().StartsWith("0x")) // fallback for unknown input
        return input;

      var hexParts = input.Trim().Split(' ');
      var buffer = new MemoryStream();

      foreach (var part in hexParts)
      {
        if (part == "" || part == "0x00")
          continue;
        buffer.WriteByte((byte)ParseInt(part));
      }

      return this.DefaultEncoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }
    #endregion

    #region DefaultEncoding
    public override Encoding DefaultEncoding
    {
      get => base.DefaultEncoding;
      set
      {
        if (value == this.DefaultEncoding)
          return;
        base.DefaultEncoding = value;
        this.ChangeEncoding();
      }
    }
    #endregion

    #region ChangeEncoding
    private void ChangeEncoding()
    {
      foreach (var list in this.DataRoot.ChannelLists)
      {
        foreach (var channel in list.Channels)
        {
          if (!(channel is Channel ch))
            continue;
          ch.Name = this.DecodeName(ch.RawName);
          ch.Satellite = this.DecodeName(ch.RawSatellite);
        }
      }
    }
    #endregion

    #region Save()
    public override void Save(string tvOutputFile)
    {
      // "Save As..." is not supported by this loader

      foreach (var list in this.DataRoot.ChannelLists)
      {
          this.UpdateChannelList(list);
      }

      foreach (var file in this.fileDataList)
        this.SaveFile(file);
    }
    #endregion

    #region SaveFile()
    private void SaveFile(FileData file)
    {
      // by default .NET reformats the whole XML. These settings produce almost same format as the TV xml files use
      var xmlSettings = new XmlWriterSettings();
      xmlSettings.Encoding = this.DefaultEncoding;
      xmlSettings.CheckCharacters = false;
      xmlSettings.Indent = true;
      xmlSettings.IndentChars = "  ";
      xmlSettings.NewLineHandling = NewLineHandling.None;
      xmlSettings.NewLineChars = file.newline;
      xmlSettings.OmitXmlDeclaration = false;

      string xml;
      using (var sw = new StringWriter())
      using (var w = new CustomXmlWriter(sw, xmlSettings, false))
      {
        file.doc.WriteTo(w);
        w.Flush();
        xml = sw.ToString();
      }

      if (file.fileType == "air")
      {
        int retVal = ConvertToBIN_Air(Encoding.ASCII.GetBytes(xml));
        if (retVal != 0)
        {
          switch (retVal)
          {
            case -1:
            case 1:
              throw new Exception("Could not write \"" + file.fileType + "\" channel list: Path not found");
            case 4:
              throw new Exception("Could not write \"" + file.fileType + "\" channel list: Error in XML string");
          }
        }
      } else if (file.fileType == "cable")
      {
        int retVal = ConvertToBIN_Cable(Encoding.ASCII.GetBytes(xml));
        if (retVal != 0)
        {
          switch (retVal)
          {
            case -1:
            case 1:
              throw new Exception("Could not write \"" + file.fileType + "\" channel list: Path not found");
            case 4:
              throw new Exception("Could not write \"" + file.fileType + "\" channel list: Error in XML string");
          }
        }
      } else if (file.fileType == "satellite")
      {
        int retVal = ConvertToBin_Satellite(Encoding.ASCII.GetBytes(xml));
        if (retVal != 0)
        {
          switch (retVal)
          {
            case -1:
            case 1:
              throw new Exception("Could not write \"" + file.fileType + "\" channel list: Path not found");
            case 4:
              throw new Exception("Could not write \"" + file.fileType + "\" channel list: Error in XML string");
          }
        }
      }
    }
    #endregion

    #region UpdateChannelList()
    private void UpdateChannelList(ChannelList list)
    {
      foreach (var channel in list.Channels)
      {
        var ch = channel as Channel;
        if (ch == null)
          continue; // might be a proxy channel from a reference list

        if (ch.IsDeleted || ch.NewProgramNr < 0)
        {
          int chCnt = ParseInt(ch.Node.ParentNode.FirstChild.InnerText);
          ch.Node.ParentNode.FirstChild.InnerText = (chCnt - 1).ToString();

          ch.Node.ParentNode.RemoveChild(ch.Node);
          continue;
        }

        foreach (XmlNode nde in ch.Node.ChildNodes)
        {
          if (nde.Name == "ChNum")
          {
            nde.InnerText = ch.NewProgramNr.ToString();
          } else if (ch.IsNameModified && nde.Name  == "ChName")
          {
            nde.InnerText = EncodeName(ch.Name);
          } else if (nde.Name == "Fav")
          {
            nde.InnerText = ch.FavIndex[0] >= 0 ? "true" : "false";
          } else if (nde.Name == "ChLock")
          { 
            nde.InnerText = ch.Lock ? "true" : "false";
          } else if (nde.Name == "UserHide")
          { 
            nde.InnerText = ch.Hidden ? "1" : "0";
          }
        }
      }
    }
    #endregion

    #region EncodeName
    private string EncodeName(string name)
    {
      var bytes = this.DefaultEncoding.GetBytes(name);
      var sb = new StringBuilder();
      foreach (var b in bytes)
        sb.Append($"0x{b:X2} 0x00 ");
      sb.Remove(sb.Length - 1, 1);
      return sb.ToString();
    }
    #endregion

    #region class FileData
    private class FileData
    {
      public String fileType;
      public XmlDocument doc;
      public string textContent;
      public string newline;
    }
    #endregion
  }
}
