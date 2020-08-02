using System.Xml;
using ChanSort.Api;

namespace ChanSort.Loader.Philips
{
  internal class Channel : ChannelInfo
  {
    public readonly XmlNode Node;
    public string RawName;
    public string RawSatellite;

    internal Channel(SignalSource source, int order, int rowId, XmlNode node)
    {
      this.SignalSource = source;
      this.RecordOrder = order;
      this.RecordIndex = rowId;
      this.Node = node;
    }
  }
}
