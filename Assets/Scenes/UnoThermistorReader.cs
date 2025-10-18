using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

// Manages Serial connection for the Arduino Uno (Thermistor)
public class UnoThermistorReader : MonoBehaviour
{
    [Header("Serial Settings (Arduino Uno)")]
    [Tooltip("Port for the Arduino Uno (Thermistor).")]
    public string portName = "/dev/cu.usbmodem1201"; // Example: Must be different from Feather port
    public int baudRate = 9600;
    [Tooltip("Serial read timeout in ms.")]
    public int readTimeoutMs = 100;

    [Header("Live Data (from Uno)")]
    public int tempValue = 0;   // Thermistor value (0-1023)

    // Internal Serial Communication State
    SerialPort _port;
    Thread _readerThread;
    volatile bool _runReader;
    readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
    string _lastError = null;

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
            Debug.LogError($"[Uno Thermistor Reader] {_lastError}");
            _lastError = null;
        }

        // Drain queued lines; parse the most recent valid one
        while (_lines.TryDequeue(out var line))
        {
            // Expected format: Temp (1 value)
            if (int.TryParse(line.Trim(), out var t))
            {
                tempValue = t;
            }
        }
    }

    // --- Port Management Methods (Simplified) ---

    void TryOpenPort()
    {
        if (string.IsNullOrEmpty(portName))
        {
            Debug.LogError("[Uno Thermistor Reader] No serial port specified.");
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
            Debug.Log($"[Uno Thermistor Reader] Opened serial port: {portName} @ {baudRate}");

            _runReader = true;
            _readerThread = new Thread(ReaderLoop) { IsBackground = true, Name = "UnoSerialReader" };
            _readerThread.Start();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Uno Thermistor Reader] Error opening serial port '{portName}': {ex.Message}");
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
}