using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

// Manages Serial connection for the Feather Sense (Controls/Light/Sound)
public class FeatherDataManager : MonoBehaviour
{
    [Header("Serial Settings (Feather Sense)")]
    [Tooltip("Port for the Feather Sense (Controls).")]
    public string portName = "/dev/cu.usbmodem1301";
    public int baudRate = 9600;
    [Tooltip("Serial read timeout in ms.")]
    public int readTimeoutMs = 100;
    public bool autoDetectPort = false;

    [Header("Live Data (from Feather Sense)")]
    public int xAxis = 0;
    public int yAxis = 0;
    public int buttonState = 1; // 0=Pressed, 1=Released
    public int lightValue = 0;  // Photoresistor
    public int soundValue = 0;  // Sound Sensor (analog reading)

    // Internal Serial Communication State
    SerialPort _port;
    Thread _readerThread;
    volatile bool _runReader;
    readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
    string _lastError = null;
    readonly ConcurrentQueue<string> _outgoing = new ConcurrentQueue<string>(); 

    void OnEnable()
    {
        TryOpenPort();
    }

    void OnDisable()
    {
        StopReaderAndClose();
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(_lastError))
        {
            Debug.LogError($"[Feather Data Manager] {_lastError}");
            _lastError = null;
        }

        // Drain queued lines; parse the most recent valid one
        while (_lines.TryDequeue(out var line))
        {
            // Expected format: X Y Button Light Sound (5 values)
            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 5) continue;

            if (
                int.TryParse(parts[0], out var x) &&
                int.TryParse(parts[1], out var y) &&
                int.TryParse(parts[2], out var b) &&
                int.TryParse(parts[3], out var l) &&
                int.TryParse(parts[4], out var s)
                )
            {
                xAxis = x;
                yAxis = y;
                buttonState = b; 
                lightValue = l;
                soundValue = s;
            }
        }
    }
    
    // --- Port Management Methods ---

    void TryOpenPort()
    {
        if (autoDetectPort)
        {
            var chosen = ChoosePort(portName);
            if (!string.IsNullOrEmpty(chosen)) portName = chosen;
        }

        if (string.IsNullOrEmpty(portName))
        {
            Debug.LogError("[Feather Data Manager] No serial port specified/found.");
            return;
        }

        try
        {
            _port = new SerialPort(portName, baudRate);
            _port.NewLine = "\n";
            _port.ReadTimeout = readTimeoutMs;
            _port.DtrEnable = true;
            _port.RtsEnable = true;

            _port.Open();
            _port.DiscardInBuffer();
            Debug.Log($"[Feather Data Manager] Opened serial port: {portName} @ {baudRate}");

            _runReader = true;
            _readerThread = new Thread(ReaderLoop) { IsBackground = true, Name = "FeatherSerialReader" };
            _readerThread.Start();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Feather Data Manager] Error opening serial port '{portName}': {ex.Message}");
            SafeClose();
        }
    }

    void ReaderLoop()
    {
        try
        {
            while (_runReader && _port != null && _port.IsOpen)
            {
                try
                {
                    string line = _port.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                        _lines.Enqueue(line);
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    _lastError = $"Serial read error: {ex.Message}";
                    break;
                }
            }
        }
        finally { }
    }

    void StopReaderAndClose()
    {
        _runReader = false;
        if (_readerThread != null)
        {
            try { _readerThread.Join(500); } catch { }
            _readerThread = null;
        }
        SafeClose();
    }

    void SafeClose()
    {
        if (_port != null)
        {
            try
            {
                if (_port.IsOpen) _port.Close();
            }
            catch { }
            finally
            {
                _port.Dispose();
                _port = null;
            }
        }
    }

    static string ChoosePort(string preferred)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        try
        {
            var ports = SerialPort.GetPortNames();
            if (!string.IsNullOrEmpty(preferred))
            {
                foreach (var p in ports) if (p == preferred) return preferred;
            }
            foreach (var p in ports)
                if (p.Contains("usbmodem") || p.Contains("usbserial"))
                    return p;
        }
        catch { }
#endif
        return preferred;
    }
}