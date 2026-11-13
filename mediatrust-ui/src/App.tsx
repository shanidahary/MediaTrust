import { useRef, useState } from "react";
import "./App.css";

type UploadResult = {
  mediaId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  objectKey: string;
  isDuplicate: boolean;
};

type DetectorResult = {
  id: string;
  mediaId: string;
  detectorName: string;
  score: number;
  details: string;
  createdAtUtc: string;
};

type AnalysisJob = {
  jobId: string;
  mediaId: string;
  objectKey: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

type AnalysisStatus =
  | "idle"
  | "uploading"
  | "analyzing"
  | "completed"
  | "failed";

function App() {
  const [file, setFile] = useState<File | null>(null);

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const [uploadResult, setUploadResult] = useState<UploadResult | null>(null);

  const [detectorResults, setDetectorResults] = useState<DetectorResult[]>([]);

  const [analysisJob, setAnalysisJob] = useState<AnalysisJob | null>(null);

  const [analysisStatus, setAnalysisStatus] = useState<AnalysisStatus>("idle");
  // --------------------------------------------------
  // Analysis History
  // --------------------------------------------------
  const [showHistory, setShowHistory] = useState(false);

  // --------------------------------------------------
  // Job History
  // --------------------------------------------------
  const [jobs, setJobs] = useState<AnalysisJob[]>([]);
  const [showJobs, setShowJobs] = useState(false);
  const [jobsLoading, setJobsLoading] = useState(false);

  const [jobSearch, setJobSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");

  // --------------------------------------------------
  // Selected Job Details
  // --------------------------------------------------
  const [selectedJob, setSelectedJob] = useState<AnalysisJob | null>(null);

  // --------------------------------------------------
  // Selected Job Analysis Results
  // --------------------------------------------------
  const [selectedJobResults, setSelectedJobResults] = useState<
    DetectorResult[]
  >([]);
  const [selectedJobResultsLoading, setSelectedJobResultsLoading] = useState(false);
  const [selectedJobResultsError, setSelectedJobResultsError] = useState("");
  const selectedJobRequestId = useRef(0);

  const wait = (milliseconds: number) =>
    new Promise((resolve) => setTimeout(resolve, milliseconds));

  const pollForAnalysis = async (mediaId: string) => {
    const maxAttempts = 30;

    setAnalysisStatus("analyzing");

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      console.log(`Checking analysis status... attempt ${attempt}`);

      // Get latest job status from Orchestrator
      const jobResponse = await fetch(
        `http://localhost:8082/jobs/media/${mediaId}`,
      );

      if (!jobResponse.ok) {
        throw new Error("Failed to get analysis job status");
      }

      const job: AnalysisJob = await jobResponse.json();

      setAnalysisJob(job);

      console.log("Job status:", job.status);

      // Analysis failed
      if (job.status === "Failed") {
        setAnalysisStatus("failed");

        setMessage("Analysis failed. Please try again.");

        return;
      }

      // Analysis completed
      if (job.status === "Completed") {
        const resultsResponse = await fetch(
          `http://localhost:8083/detectors/results/${mediaId}`,
        );

        if (!resultsResponse.ok) {
          throw new Error("Failed to get analysis results");
        }

        const results: DetectorResult[] = await resultsResponse.json();

        setDetectorResults(results);
        setAnalysisStatus("completed");

        return;
      }

      // Pending / Processing
      await wait(1000);
    }

    setAnalysisStatus("failed");

    setMessage("Analysis is taking longer than expected.");
  };

  const handleUpload = async () => {
    if (!file) return;

    try {
      setLoading(true);
      setMessage("");
      setUploadResult(null);
      setDetectorResults([]);
      setAnalysisJob(null);
      setAnalysisStatus("uploading");

      const formData = new FormData();

      formData.append("file", file);

      const response = await fetch("http://127.0.0.1:8081/media", {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const errorText = await response.text();

        throw new Error(errorText || "Upload failed");
      }

      const data: UploadResult = await response.json();

      console.log("Upload result:", data);

      setUploadResult(data);

      if (data.isDuplicate) {
        setLoading(false);
        setAnalysisStatus("idle");
        return;
      }

      setLoading(false);

      await pollForAnalysis(data.mediaId);
    } catch (error) {
      console.error("MediaTrust error:", error);

      setAnalysisStatus("failed");

      if (error instanceof Error) {
        setMessage(error.message);
      } else {
        setMessage("Something went wrong.");
      }

      setLoading(false);
    }
  };

  // --------------------------------------------------
  // Analyze Existing Media Again
  // --------------------------------------------------

  const handleAnalyzeAgain = async () => {
    if (!uploadResult) return;

    try {
      setMessage("");
      setDetectorResults([]);
      setAnalysisJob(null);
      setShowHistory(false);
      setAnalysisStatus("analyzing");

      const response = await fetch("http://localhost:8082/jobs", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          mediaId: uploadResult.mediaId,
          objectKey: uploadResult.objectKey,
          contentType: uploadResult.contentType,
          sizeBytes: uploadResult.sizeBytes,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();

        throw new Error(errorText || "Failed to start analysis");
      }

      await pollForAnalysis(uploadResult.mediaId);
    } catch (error) {
      console.error("Analyze again error:", error);

      setAnalysisStatus("failed");

      if (error instanceof Error) {
        setMessage(error.message);
      } else {
        setMessage("Failed to start analysis.");
      }
    }
  };

  // --------------------------------------------------
  // Load Job History
  // --------------------------------------------------
  const handleLoadJobs = async () => {
    try {
      setJobsLoading(true);
      setMessage("");

      const response = await fetch("http://localhost:8082/jobs");

      if (!response.ok) {
        throw new Error("Failed to load job history");
      }

      const data: AnalysisJob[] = await response.json();

      setJobs(data);
      setShowJobs(true);
    } catch (error) {
      console.error("Job history error:", error);

      if (error instanceof Error) {
        setMessage(error.message);
      } else {
        setMessage("Failed to load job history.");
      }
    } finally {
      setJobsLoading(false);
    }
  };

  // --------------------------------------------------
  // Load Selected Job Analysis Results
  // --------------------------------------------------
  const handleSelectJob = async (job: AnalysisJob) => {
    const requestId = ++selectedJobRequestId.current;
    setSelectedJob(job);
    setSelectedJobResults([]);
    setSelectedJobResultsLoading(true);
    setSelectedJobResultsError("");

    try {
      const response = await fetch(
        `http://localhost:8083/detectors/results/job/${job.jobId}`,
      );

      if (response.status === 404) return;

      if (!response.ok) {
        throw new Error("Failed to load job analysis results");
      }

      const results: DetectorResult[] = await response.json();

      // Ignore responses from selections that have since been replaced.
      if (requestId === selectedJobRequestId.current) {
        setSelectedJobResults(results);
      }
    } catch (error) {
      console.error("Failed to load selected job results:", error);
      if (requestId === selectedJobRequestId.current) {
        setSelectedJobResultsError("Failed to load job analysis results.");
      }
    } finally {
      if (requestId === selectedJobRequestId.current) {
        setSelectedJobResultsLoading(false);
      }
    }
  };

  // --------------------------------------------------
  // Sort Analysis Results - Latest First
  // --------------------------------------------------
  const sortedResults = [...detectorResults].sort(
    (a, b) =>
      new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime(),
  );

  const latestResult = sortedResults[0];
  const previousResults = sortedResults.slice(1);

  // --------------------------------------------------
  // Filter Job History
  // --------------------------------------------------
  const filteredJobs = jobs.filter((job) => {
    const fileName = job.objectKey.split("/").pop()?.toLowerCase() ?? "";

    const matchesSearch =
      fileName.includes(jobSearch.toLowerCase()) ||
      job.mediaId.toLowerCase().includes(jobSearch.toLowerCase());

    const matchesStatus = statusFilter === "All" || job.status === statusFilter;

    return matchesSearch && matchesStatus;
  });

  return (
    <div className="page">
      <div className="card">
        <h1>MediaTrust</h1>

        <p className="subtitle">Upload an image or video for media analysis</p>

        <div className="upload-box">
          <input
            type="file"
            accept="image/*,video/*"
            onChange={(e) => {
              const selectedFile = e.target.files?.[0];

              if (selectedFile) {
                const maxFileSize = 500 * 1024 * 1024;

                if (selectedFile.size > maxFileSize) {
                  setMessage("File is too large. Maximum size is 500 MB.");

                  setFile(null);
                  return;
                }
                setFile(selectedFile);

                setMessage("");
                setUploadResult(null);
                setDetectorResults([]);
                setAnalysisJob(null);
                setAnalysisStatus("idle");
                setShowHistory(false);
              }
            }}
          />

          {file && (
            <div className="file-info">
              <strong>Selected file:</strong>
              <p>{file.name}</p>
            </div>
          )}
        </div>

        <button
          disabled={!file || loading || analysisStatus === "analyzing"}
          onClick={handleUpload}
        >
          {analysisStatus === "uploading"
            ? "Uploading..."
            : analysisStatus === "analyzing"
              ? "Analyzing..."
              : "Analyze Media"}
        </button>

        {/* --------------------------------------------------
        Job History Button
        -------------------------------------------------- */}
        <button
          onClick={() => {
            if (showJobs) {
              setShowJobs(false);
            } else {
              handleLoadJobs();
            }
          }}
          disabled={jobsLoading}
        >
          {jobsLoading
            ? "Loading Job History..."
            : showJobs
              ? "Hide Job History"
              : "View Job History"}
        </button>

        {/* --------------------------------------------------
         Job History Dashboard
        -------------------------------------------------- */}
        {showJobs && (
          <div className="job-history">
            <h2>Job History</h2>
            <p className="jobs-count">
              Showing {filteredJobs.length} of {jobs.length} jobs
            </p>
            <div className="job-filters">
              <input
                type="text"
                placeholder="Search by file name or Media ID..."
                value={jobSearch}
                onChange={(e) => setJobSearch(e.target.value)}
              />

              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
              >
                <option value="All">All Statuses</option>
                <option value="Pending">Pending</option>
                <option value="Processing">Processing</option>
                <option value="Completed">Completed</option>
                <option value="Failed">Failed</option>
              </select>
            </div>

            {jobs.length === 0 ? (
              <p>No analysis jobs found.</p>
            ) : (
              <div className="jobs-table-wrapper">
                <table className="jobs-table">
                  <thead>
                    <tr>
                      <th>File Name</th>
                      <th>Status</th>
                      <th>Created</th>
                    </tr>
                  </thead>

                  <tbody>
                    {filteredJobs.map((job) => (
                      <tr
                        key={job.jobId}
                        onClick={() => handleSelectJob(job)}
                        className="job-row"
                      >
                        <td>{job.objectKey.split("/").pop()}</td>

                        <td>
                          <span
                            className={`status-badge status-${job.status.toLowerCase()}`}
                          >
                            {job.status}
                          </span>
                        </td>

                        <td>{new Date(job.createdAtUtc).toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {/* --------------------------------------------------
                Selected Job Details
                -------------------------------------------------- */}
                {selectedJob && (
                  <div className="job-details">
                    <h2>Job Details</h2>

                    <p>
                      <strong>File:</strong>{" "}
                      {selectedJob.objectKey.split("/").pop()}
                    </p>

                    <p>
                      <strong>Job ID:</strong> {selectedJob.jobId}
                    </p>

                    <p>
                      <strong>Media ID:</strong> {selectedJob.mediaId}
                    </p>

                    <p>
                      <strong>Status:</strong> {selectedJob.status}
                    </p>

                    <p>
                      <strong>Created:</strong>{" "}
                      {new Date(selectedJob.createdAtUtc).toLocaleString()}
                    </p>

                    <p>
                      <strong>Updated:</strong>{" "}
                      {selectedJob.updatedAtUtc
                        ? new Date(selectedJob.updatedAtUtc).toLocaleString()
                        : "-"}
                    </p>
                  <div className="selected-job-analysis">
                    <h3>Analysis Result</h3>

                    {selectedJobResultsLoading ? (
                      <p>Loading analysis result...</p>
                    ) : selectedJobResultsError ? (
                      <p className="error-message">{selectedJobResultsError}</p>
                    ) : selectedJobResults.length === 0 ? (
                      <p>No job-specific analysis result is available for this analysis.</p>
                    ) : selectedJobResults.map((result) => (
                      <div key={result.id}>
                        <p>
                          <strong>Detector:</strong> {result.detectorName}
                        </p>

                        <p>
                          <strong>Risk Score:</strong>{" "}
                          {(result.score * 100).toFixed(0)}%
                        </p>

                        <p>
                          <strong>Details:</strong> {result.details}
                        </p>

                        <p>
                          <strong>Analyzed:</strong>{" "}
                          {new Date(result.createdAtUtc).toLocaleString()}
                        </p>
                      </div>
                    ))}
                  </div>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {uploadResult &&
          (!uploadResult.isDuplicate || analysisStatus !== "idle") && (
            <div className="analysis-status">
              <h3>Analysis Status</h3>

              <p>✓ File uploaded</p>

              <p>
                <strong>Status:</strong>{" "}
                {analysisJob?.status ??
                  (analysisStatus === "failed" ? "Failed" : "Pending")}
              </p>
            </div>
          )}
        {message && <p className="error-message">{message}</p>}

        {uploadResult && (
          <div className="result">
            {uploadResult.isDuplicate ? (
              <>
                <h2>This file was already uploaded</h2>

                <p>
                  <strong>File:</strong> {uploadResult.fileName}
                </p>

                <p>
                  <strong>Media ID:</strong> {uploadResult.mediaId}
                </p>

                <p>
                  <strong>Type:</strong> {uploadResult.contentType}
                </p>

                <p>
                  <strong>Size:</strong>{" "}
                  {(uploadResult.sizeBytes / 1024).toFixed(2)} KB
                </p>

                <button
                  onClick={handleAnalyzeAgain}
                  disabled={analysisStatus === "analyzing"}
                >
                  {analysisStatus === "analyzing"
                    ? "Analyzing..."
                    : "Analyze Again"}
                </button>
              </>
            ) : (
              <>
                <h2>Upload Successful ✓</h2>

                <p>
                  <strong>File:</strong> {uploadResult.fileName}
                </p>

                <p>
                  <strong>Media ID:</strong> {uploadResult.mediaId}
                </p>

                <p>
                  <strong>Type:</strong> {uploadResult.contentType}
                </p>

                <p>
                  <strong>Size:</strong>{" "}
                  {(uploadResult.sizeBytes / 1024).toFixed(2)} KB
                </p>
              </>
            )}
          </div>
        )}

        {analysisStatus === "completed" && latestResult && (
          <div className="analysis-results">
            <h2>Latest Analysis ✓</h2>

            <div className="detector-result">
              <h3>{latestResult.detectorName}</h3>

              <p>
                <strong>Risk Score:</strong>{" "}
                {(latestResult.score * 100).toFixed(0)}%
              </p>

              <p>
                <strong>Details:</strong> {latestResult.details}
              </p>

              <p>
                <strong>Analyzed:</strong>{" "}
                {new Date(latestResult.createdAtUtc).toLocaleString()}
              </p>
            </div>

            {previousResults.length > 0 && (
              <>
                <button onClick={() => setShowHistory(!showHistory)}>
                  {showHistory
                    ? "Hide Analysis History"
                    : `View Analysis History (${previousResults.length})`}
                </button>

                {showHistory && (
                  <div className="analysis-history">
                    <h2>Analysis History</h2>

                    {previousResults.map((result) => (
                      <div className="detector-result" key={result.id}>
                        <h3>{result.detectorName}</h3>

                        <p>
                          <strong>Risk Score:</strong>{" "}
                          {(result.score * 100).toFixed(0)}%
                        </p>

                        <p>
                          <strong>Details:</strong> {result.details}
                        </p>

                        <p>
                          <strong>Analyzed:</strong>{" "}
                          {new Date(result.createdAtUtc).toLocaleString()}
                        </p>
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
