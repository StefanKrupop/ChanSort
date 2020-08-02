using ChanSort.Api;

namespace ChanSort.Loader.Philips
{
  public class SerializerPlugin : ISerializerPlugin
  {
    public string DllName { get; set; }
    public string PluginName => "Philips .dat";
    public string FileFilter => "*.dat;*.bin";

    public SerializerBase CreateSerializer(string inputFile)
    {
      return new Serializer(inputFile);
    }
  }
}
