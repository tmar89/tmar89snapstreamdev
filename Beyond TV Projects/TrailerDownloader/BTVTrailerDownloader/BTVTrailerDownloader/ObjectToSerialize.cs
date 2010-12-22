using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable()]
public class ObjectToSerialize : ISerializable
{
    private List<string> downloadedTrailers;

    public List<string> DownloadedTrailers
    {
        get { return this.downloadedTrailers; }
        set { this.downloadedTrailers = value; }
    }

    public ObjectToSerialize()
    {
    }

    public ObjectToSerialize(SerializationInfo info, StreamingContext ctxt)
    {
        this.downloadedTrailers = (List<string>)info.GetValue("DownloadedTrailers", typeof(List<string>));
    }

    public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
    {
        info.AddValue("DownloadedTrailers", this.downloadedTrailers);
    }
}