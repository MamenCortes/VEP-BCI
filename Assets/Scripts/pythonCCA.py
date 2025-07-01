# cca_bci.py
import time
import numpy as np
from sklearn.cross_decomposition import CCA
from pylsl import StreamInlet, StreamInfo, StreamOutlet
import pylsl
import sys
import os
from collections import defaultdict, deque
import threading
import queue # Using the thread-safe queue module

# TODO: delete all the debugging print lines!

# --- LSL Configuration ---
LSL_EEG_STREAM_NAME = 'g.USBamp' #'LSLExampleAmp' #'g.USBamp'
LSL_MARKER_STREAM_NAME = 'Unity.MarkerStream'
LSL_OUTPUT_STREAM_NAME = 'CCA_Classifier_Output'
MODEL_DIR = 'saved_models' # Directory to save models

# --- Default BCI Parameters (will be overwritten by Unity) ---
DEFAULT_CONFIG = {
    'subjectID': 'default_subject',
    'windowSize': 2.0,
    'windowStep': 0.5,
    'n_harmonics': 2,
    'confidenceThreshold': 0.01,
    'frequencies': [6., 6.67, 7.5, 8.57]
}

class SSVEP_CCA:
    """Initialize Canonical Correlation Analysis for BCI."""
    def __init__(self, frequencies, srate, window_size_samples, n_harmonics=2, model_path='cca_model.npz'):
        self.frequencies = np.array(frequencies)
        self.srate = srate
        self.window_size_samples = window_size_samples
        self.n_harmonics = n_harmonics
        self.model_path = model_path
        self.n_freqs = len(frequencies)
        self.user_templates = None
        self.cca_model_ref = CCA(n_components=1)

        print(f"CCA model initialized for frequencies: {self.frequencies} Hz")
        self.load_model()

    def _generate_reference_signals(self, n_samples):
        """Generates sine-cosine reference signals for given frequencies."""
        T = n_samples / self.srate
        t = np.arange(0, T, 1.0 / self.srate)
        reference_signals = []
        for freq in self.frequencies:
            ref = []
            for h in range(1, self.n_harmonics + 1):
                ref.append(np.sin(2 * np.pi * h * freq * t))
                ref.append(np.cos(2 * np.pi * h * freq * t))
            reference_signals.append(np.array(ref).T)
        return reference_signals

    def train(self, training_data):
        """
        Creates user-specific templates by averaging trials.
        The "training" is learning the prototypical SSVEP response.

        Args:
            training_data (dict): A dictionary where keys are frequencies (float)
                                  and values are EEG data segments (n_total_samples, n_channels).
        """
        print("Training user-specific model by creating averaged templates...")
        self.user_templates = {}
        
        for freq, eeg_segment in training_data.items():
            if eeg_segment.shape[0] < self.window_size_samples:
                print(f"Warning: Not enough data for {freq} Hz to create a template. Skipping.", file=sys.stderr)
                continue

            # Segment the long recording into non-overlapping trials of the correct window size
            n_trials = eeg_segment.shape[0] // self.window_size_samples
            if n_trials == 0:
                print(f"Warning: Data for {freq} Hz is shorter than one window. Skipping.", file=sys.stderr)
                continue
            
            segmented_trials = eeg_segment[:n_trials * self.window_size_samples].reshape(
                n_trials, self.window_size_samples, eeg_segment.shape[1]
            )
            
            # The template is the average across all trials for this frequency
            template = np.mean(segmented_trials, axis=0)
            self.user_templates[freq] = template
            print(f"Created template for {freq} Hz from {n_trials} trials.")
            
        self.save_model()

    def save_model(self):
        """Saves the learned templates and metadata to a .npz file."""
        # TODO: check if this is better or saving a trained CCA instance
        if self.user_templates:
            os.makedirs(os.path.dirname(self.model_path), exist_ok=True)
            templates_to_save = {str(k): v for k, v in self.user_templates.items()}
            np.savez(
                self.model_path, 
                **templates_to_save,
                frequencies=self.frequencies,
                srate=self.srate,
                window_size_samples=self.window_size_samples
            )
            print(f"User templates saved to {self.model_path}")

    def load_model(self):
        """Loads learned templates from a .npz file."""
        try:
            data = np.load(self.model_path, allow_pickle=True)
            # Check for consistency
            if (not np.array_equal(data['frequencies'], self.frequencies) or 
                data['srate'] != self.srate or 
                data['window_size_samples'] != self.window_size_samples):
                print("Warning: Model file params do not match config. Ignoring.", file=sys.stderr)
                self.user_templates = None
                return

            self.user_templates = {float(k): v for k, v in data.items() if k not in ['frequencies', 'srate', 'window_size_samples']}
            print(f"User-trained templates loaded successfully from {self.model_path}")

        except FileNotFoundError:
            print("No training file found. Using reference signal CCA mode.")
            self.user_templates = None
            
 
    def predict(self, eeg_chunk):
        """
        Predict the target frequency from an incoming EEG chunk
        """
        correlations = []
        n_samples_eeg = eeg_chunk.shape[0]

        if self.user_templates:
            # User-Trained Template-Based CCA     
            cca = CCA(n_components=1)

            for i, freq in enumerate(self.frequencies):
                if freq not in self.user_templates:
                    correlations.append(0.0)
                    continue
            
                full_user_template = self.user_templates[freq]
                n_samples_template = full_user_template.shape[0]
                if n_samples_eeg == n_samples_template:
                    adapted_template = full_user_template
                elif n_samples_eeg < n_samples_template:
                    adapted_template = full_user_template[:n_samples_eeg, :]
                else:
                    n_repeats = int(np.ceil(n_samples_eeg / n_samples_template))
                    tiled_template = np.tile(full_user_template, (n_repeats, 1))
                    adapted_template = tiled_template[:n_samples_eeg, :]
                
                ref_signal = reference_signals[i]
                cca.fit(eeg_chunk, ref_signal)
                x_test_c, _ = cca.transform(eeg_chunk, ref_signal)

                cca.fit(adapted_template, ref_signal)
                x_template_c, _ = cca.transform(adapted_template, ref_signal)
                
                corr = np.corrcoef(x_test_c.T, x_template_c.T)[0, 1]
                correlations.append(corr)

        else:
            # If no trained model: use standard reference-signals-based CCA
            reference_signals = self._generate_reference_signals(n_samples_eeg)
            for ref_signal in reference_signals:
                self.cca_model_ref.fit(eeg_chunk, ref_signal)
                U, V = self.cca_model_ref.transform(eeg_chunk, ref_signal)
                corr = np.corrcoef(U[:, 0], V[:, 0])[0, 1]
                correlations.append(corr)

        predicted_idx = np.argmax(correlations)
        return self.frequencies[predicted_idx], np.array(correlations)

# --- THREAD-SAFE REAL-TIME FUNCTIONS ---
def eeg_producer_thread(eeg_inlet, data_queue, stop_event):
    """Producer thread: continuously pulls data from LSL and puts it in a queue."""
    print("EEG producer thread started.")
    srate = eeg_inlet.info().nominal_srate()
    max_samples_to_pull = int(srate) # Convert float to int
    
    while not stop_event.is_set():
        # Pull up to 1 second of data at a time using the integer max_samples value.
        chunk, _ = eeg_inlet.pull_chunk(timeout=1.0, max_samples=max_samples_to_pull)
        if chunk:
            data_queue.put(np.array(chunk))
    print("EEG producer thread stopped.")


def handle_testing_session(cca_model, eeg_inlet, marker_inlet, outlet, config, freq_to_stimulus_map):
    """
    Consumer: using session ('startTest') and trial ('startStimulation') commands.
    """
    print("\n--- Testing Session Active. Waiting for 'startStimulation' or 'stopTest'. ---")
    
    eeg_inlet.pull_chunk(timeout=0.0) 
    print("LSL inlet buffer cleared.")

    srate = cca_model.srate
    min_window_samples = int(0.5 * srate) #TODO 
    max_window_samples = int(1.5 * srate) #TODO
    step_samples = int(0.1 * srate)
    
    data_queue = queue.Queue()
    stop_event = threading.Event()
    producer = threading.Thread(target=eeg_producer_thread, args=(eeg_inlet, data_queue, stop_event))
    producer.daemon = True
    producer.start()

    try:
        # Session (stimulation level) loop
        while not stop_event.is_set():
            
            print("\n(Session) Waiting for 'startStimulation' marker...")
            marker, _ = marker_inlet.pull_sample()
            if not marker: continue
            
            command = marker[0]

            if command == 'stopTest':
                print("--- Received 'stopTest'. Ending testing session. ---")
                outlet.push_sample(["0"])
                break
            
            if command != 'startStimulation':
                print(f"Ignoring unknown marker '{command}' while waiting for stimulation.")
                continue

            print("Received 'startStimulation'. Beginning classification attempts for this trial...")
            
            while not stop_event.is_set():
                eeg_buffer = deque()
                
                while not data_queue.empty(): 
                    data_queue.get_nowait()
                
                # Collect the initial minimum window
                while len(eeg_buffer) < min_window_samples:
                    try: 
                        chunk = data_queue.get(timeout=0.1); eeg_buffer.extend(chunk)
                    except queue.Empty: 
                        continue

                successful_classification = False
                while True:
                    eeg_window = np.array(eeg_buffer)
                    print(f"  (Attempt) Window size: {len(eeg_window) / srate:.2f}s...", end='\r')
                    
                    predicted_freq, correlations = cca_model.predict(eeg_window)
                    sorted_corrs = np.sort(correlations)[::-1]
                    confidence = sorted_corrs[0] - sorted_corrs[1]
                    
                    # succes:
                    if confidence > config['confidenceThreshold']:
                        stimulus_id = freq_to_stimulus_map[predicted_freq]
                        output_marker = str(stimulus_id)
                        outlet.push_sample([output_marker])
                        outlet.push_sample(["0"]) # Send reset signal
                        print(f"\nSUCCESS! Classification: {output_marker}. Waiting for next trial.")
                        successful_classification = True
                        break # Exit the expanding window loop

                    # max window reached
                    if len(eeg_window) >= max_window_samples:
                        print(f"\n  TIMEOUT. Resetting and retrying for this trial.")
                        break
                    
                    # collecting more data
                    target_size = len(eeg_buffer) + step_samples
                    while len(eeg_buffer) < target_size:
                        try: chunk = data_queue.get(timeout=0.1); eeg_buffer.extend(chunk)
                        except queue.Empty: continue
               
                # If we had a success, break loop to wait for the next 'startStimulation'
                if successful_classification:
                    break
                
                # otherwise we go in again to collect more, unless stopTest is send
                marker, _ = marker_inlet.pull_sample(timeout=0.0)
                if marker and marker[0] == 'stopTest':
                    stop_event.set()

    finally:
        print("\nCleaning up testing session resources...")
        stop_event.set()
        producer.join()
        print("Resources cleaned up.")

def connect_to_lsl():
    print("Looking for EEG stream...")
    eeg_inlet = StreamInlet(pylsl.resolve.resolve_stream('name', LSL_EEG_STREAM_NAME)[0])
    srate = int(eeg_inlet.info().nominal_srate())
    print(f"EEG stream found ({srate} Hz).")
    print("Looking for Marker stream...")
    marker_inlet = StreamInlet(pylsl.resolve.resolve_stream('name', LSL_MARKER_STREAM_NAME)[0])
    print("Marker stream found.")
    return eeg_inlet, marker_inlet, srate

def get_config_from_unity(marker_inlet):
    print("\n--- Waiting for configuration from Unity ---")
    config = DEFAULT_CONFIG.copy()
    while True:
        marker, _ = marker_inlet.pull_sample()
        if not marker: 
            continue
        marker_str = marker[0]
        if marker_str == 'config_done':
            print("--- Configuration received. ---")
            break
        parts = marker_str.split(':', 1)
        if len(parts) == 2:
            key, value = parts[0].strip(), parts[1].strip()
            if key in config:
                try:
                    if key == 'frequencies': 
                        config[key] = [float(f.strip()) for f in value.split(',')]
                    elif key == 'subjectID': 
                        config[key] = value
                    else: 
                        config[key] = float(value)
                    print(f"  > Received config: {key} = {config[key]}")
                except ValueError: 
                    print(f"Warning: Could not parse {key}: '{value}'", file=sys.stderr)
            else: 
                print(f"Warning: Unknown config key: {key}", file=sys.stderr)
    return config

def handle_training_session(cca_model, eeg_inlet, marker_inlet, stimulus_map):
    print("\n--- Training Session Active ---")
    training_data_chunks = defaultdict(list)
    current_stimulus_id = 0
    eeg_inlet.pull_chunk()
    while True:
        marker, _ = marker_inlet.pull_sample(timeout=0.0)
        if marker:
            marker_str = marker[0]
            if marker_str == 'stopTraining':
                print("--- Received 'stopTraining'. Finalizing. ---")
                break
            try:
                stimulus_id = int(marker_str)
                if stimulus_id in stimulus_map:
                    if stimulus_id != current_stimulus_id:
                        current_stimulus_id = stimulus_id
                        print(f"Collecting for stimulus: {current_stimulus_id} ({stimulus_map[current_stimulus_id]} Hz)")
                elif stimulus_id == 0:
                    if current_stimulus_id != 0: 
                        current_stimulus_id = 0
                        print("Cue is 0, pausing.")
            except ValueError: 
                pass
        chunk, _ = eeg_inlet.pull_chunk()
        if chunk and current_stimulus_id != 0:
            training_data_chunks[current_stimulus_id].append(np.array(chunk))
        time.sleep(0.01)
    if not training_data_chunks: 
        print("No training data collected.", file=sys.stderr)
        return
    final_training_data = {}
    for stimulus_id, chunks in training_data_chunks.items():
        freq = stimulus_map[stimulus_id]
        if chunks:
            full_segment = np.concatenate(chunks, axis=0)
            final_training_data[freq] = full_segment
            print(f"Collected {full_segment.shape[0] / cca_model.srate:.2f}s for {freq} Hz.")
    cca_model.train(final_training_data)

def main_loop(config, srate, eeg_inlet, marker_inlet):
    frequencies = config['frequencies']
    window_size_samples = int(config['windowSize'] * srate)
    stimulus_map = {i + 1: freq for i, freq in enumerate(frequencies)}
    freq_to_stimulus_map = {v: k for k, v in stimulus_map.items()}
    model_path = os.path.join(MODEL_DIR, f"model_{config['subjectID']}.npz")
    cca_model = SSVEP_CCA(
        frequencies=frequencies,
        srate=srate,
        window_size_samples=window_size_samples,
        n_harmonics=int(config['n_harmonics']),
        model_path=model_path
    )
    info = StreamInfo(LSL_OUTPUT_STREAM_NAME, 'Markers', 1, 0, 'string', 'cca_classifier')
    outlet = StreamOutlet(info)
    print(f"LSL Output stream '{LSL_OUTPUT_STREAM_NAME}' created.\n--- Initialization Complete ---")
    while True:
        try:
            print("\nSystem is IDLE. Waiting for command...")
            marker, _ = marker_inlet.pull_sample()
            if not marker: 
                continue
            command = marker[0]
            if command == 'startTraining': 
                handle_training_session(cca_model, eeg_inlet, marker_inlet, stimulus_map)
            elif command == 'startTest': 
                handle_testing_session(cca_model, eeg_inlet, marker_inlet, outlet, config, freq_to_stimulus_map)
            else: 
                print(f"Ignoring command '{command}' while idle.")
        except Exception as e:
            print(f"\nError in main loop: {e}", file=sys.stderr)
            time.sleep(1)

if __name__ == "__main__":
    try:
        eeg_inlet, marker_inlet, srate = connect_to_lsl()
        config = get_config_from_unity(marker_inlet)
        print("\nFinal Configuration:")
        for key, value in config.items():
            print(f"  - {key}: {value}")
        main_loop(config, srate, eeg_inlet, marker_inlet)
    except Exception as e:
        print(f"\nA critical error occurred: {e}", file=sys.stderr)
    finally:
        print("\nBCI script shutting down.")