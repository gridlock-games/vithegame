using System.Collections;
using UnityEngine;
using static ViNetAnalytics.Deviceinfo;

namespace ViNetAnalytics
{
  public class ViAnalytics : MonoBehaviour
  {
    private const string APIURL = "154.90.35.191/";
    public Deviceinfo capturedData;
    public bool dataPrivacyAnonymousPermission = true;

    public IEnumerator DoDataCaptureCommunication()
    {
      //Legal: Data privacy verification
      if (dataPrivacyAnonymousPermission == false) //Replace with legal stuff
      {
        yield break; //No permission was granted voiding transfer
      }
      //Start Capturing
      yield return capturedData = new Deviceinfo()
      {
        deviceModel = SystemInfo.deviceModel,
        deviceType = SystemInfo.deviceType,
        OperatingSystem = new DeviceInfoOperatingSystem()
        {
          osFamily = SystemInfo.operatingSystemFamily.ToString(),
          osVersion = SystemInfo.operatingSystem
        },
        Processor = new DeviceInfoProcessor()
        {
          processorType = SystemInfo.processorType,
          processorFrequency = SystemInfo.processorFrequency,
          processorLogicCount = SystemInfo.processorCount
        },
        Memory = new DeviceInfoMemory()
        {
          systemMemorySize = SystemInfo.systemMemorySize
        },
        Graphics = new DeviceInfoGraphics()
        {
          graphicsVendor = SystemInfo.graphicsDeviceVendor,
          graphicsModel = SystemInfo.graphicsDeviceName,
          graphicsVersion = SystemInfo.graphicsDeviceVersion,
          graphicsMemorySize = SystemInfo.graphicsMemorySize,
          isMultiThreaded = SystemInfo.graphicsMultiThreaded,
          canRayTrace = SystemInfo.supportsRayTracing,
          HDRFlags = SystemInfo.hdrDisplaySupportFlags,
          graphicsAPIType = SystemInfo.graphicsDeviceType.ToString()
        }
      };

    }
  }

  public class Deviceinfo
  {
    public string deviceModel;
    public DeviceType deviceType;

    public DeviceInfoOperatingSystem OperatingSystem;
    public DeviceInfoProcessor Processor;
    public DeviceInfoMemory Memory;
    public DeviceInfoGraphics Graphics;

    [System.Serializable]
    public class DeviceInfoOperatingSystem
    {
      public string osFamily;
      public string osVersion;
    }

    [System.Serializable]
    public class DeviceInfoProcessor
    {
      //public string processorModel;
      //public string processorManufacturer;
      public string processorType;

      public int processorLogicCount;
      public int processorFrequency;
    }

    [System.Serializable]
    public class DeviceInfoMemory
    {
      public int systemMemorySize;
    }

    [System.Serializable]
    public class DeviceInfoGraphics
    {
      public string graphicsVendor;
      public string graphicsModel;
      public string graphicsVersion;

      public int graphicsMemorySize;
      public bool isMultiThreaded;
      public bool canRayTrace;
      public HDRDisplaySupportFlags HDRFlags;

      public string graphicsAPIType; //Example case OpenGL/Metal/DirectX
    }
  }
}