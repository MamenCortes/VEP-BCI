from pylsl import StreamInfo, StreamOutlet, StreamInlet, resolve_streams
import threading
import keyboard  # pip install keyboard

# Create the classification LSL outlet
info = StreamInfo(name='Python.ClassificationStream',
                  type='Python.Classification',
                  channel_count=1,
                  nominal_srate=0,
                  channel_format='string',
                  source_id='classification_001')
outlet = StreamOutlet(info)
print("[INFO] Classification outlet created.")

# Resolve the Unity marker stream
print("[INFO] Waiting for Unity.MarkerStream...")
marker_streams = resolve_streams()
inlet = StreamInlet(marker_streams[0])
inlet.open_stream()
#sinfo = inlet.get_sinfo()  # retrieve stream information with all properties
print("[INFO] Connected to Unity.MarkerStream.")

# Function to listen for keypresses and send classifications
def send_classification():
    print("[INFO] Press A, B, or C to send classification.")
    while True:
        if keyboard.is_pressed('a'):
            outlet.push_sample(['1'])
            print("Sent: 1 (A)")
            keyboard.wait('a', suppress=True)  # Wait until 'a' is released

        elif keyboard.is_pressed('b'):
            outlet.push_sample(['2'])
            print("Sent: 2 (B)")
            keyboard.wait('b', suppress=True)

        elif keyboard.is_pressed('c'):
            outlet.push_sample(['3'])
            print("Sent: 3 (C)")
            keyboard.wait('c', suppress=True)

# Function to receive and print markers from Unity
def receive_markers():
    while True:
        sample, timestamp = inlet.pull_sample(timeout=0.0)
        if sample:
            print(f"[Unity Marker @ {timestamp:.3f}]: {sample[0]}")

# Run both threads
threading.Thread(target=send_classification, daemon=True).start()
threading.Thread(target=receive_markers, daemon=True).start()

# Keep the main thread alive
try:
    while True:
        pass
except KeyboardInterrupt:
    print("\n[INFO] Exiting.")
