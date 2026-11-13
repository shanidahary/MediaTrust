import { useState } from "react";
export type DetectorResult = {
  id: string;
  jobId?: string | null;
  mediaId: string;
  detectorName: string;
  score: number;
  details: string;
  createdAtUtc: string;
};
export function StatusBadge({ status }: {
  status: string;
}) {
  return <span className={`status-badge status-${status.toLowerCase()}`}>
    <span aria-hidden="true" />
    {status}
  </span>;
}
export function DetectorCard({ result }: {
  result: DetectorResult;
}) {
  // Presentation thresholds match the existing report service. Scores are unchanged.
  const risk = result.score < 0.3 ? "low" : result.score < 0.7 ? "medium" : "high";
  const percentage = (result.score * 100).toFixed(0);
  return (
    <article className={`detector-result risk-${risk}`}>
      <div className="result-heading">
        <div>
          <span className="eyebrow">Detector</span>
          <h3>
            {result.detectorName}
          </h3>
        </div>
        <span className="risk-label">{risk} risk</span>
      </div>
      <div className="risk-score">
        <strong>
          {percentage}
          <span>%</span>
        </strong>
        <span>Risk Score</span>
      </div>
      <meter className="risk-meter" min={0} max={1} value={result.score} aria-label={`Risk Score: ${percentage}%`} />
      <div className="risk-scale">
        <span>Lower risk</span>
        <span>Higher risk</span>
      </div>
      <div className="technical-details">
        <h4>Technical details</h4>
        <p>
          {result.details || "No technical details were provided."}
        </p>
      </div>
      <p className="analysis-time">Analyzed {new Date(result.createdAtUtc).toLocaleString()}</p>
    </article>
  );
}
export function AiExplanation({ jobId }: {
  jobId: string;
}) {
  const [explanation, setExplanation] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const loadExplanation = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await fetch(`http://localhost:8084/reports/explanation/${jobId}`);
      if (response.status === 404) {
        setError("An AI explanation is not available for this job yet.");
        return;
      }
      if (!response.ok) throw new Error("Explanation request failed");
      const data: {
        jobId: string;
        explanation: string;
      } = await response.json();
      if (data.jobId !== jobId || !data.explanation?.trim()) throw new Error("Invalid explanation response");
      setExplanation(data.explanation);
    } catch (error) {
      console.error("AI explanation error:", error);
      setError("We couldn’t load the explanation. Please try again.");
    } finally {
      setLoading(false);
    }
  };
  return (
    <section className="ai-explanation" aria-busy={loading}>
      <div className="section-heading">
        <div>
          <span className="eyebrow">Powered by Gemini</span>
          <h3>AI Explanation</h3>
        </div>
        <span className="ai-label">AI-generated</span>
      </div>
      <p className="muted">A plain-language interpretation of this job’s detector findings.</p>
      <div aria-live="polite">
        {explanation && <div className="explanation-copy">
          {explanation.split(/\n\s*\n/).map((paragraph, index) => <p key={index}>
            {paragraph}
          </p>)}
        </div>}
        {error && <p className="inline-error" role="alert">
          {error}
        </p>}
      </div>
      {!explanation && <button className="button secondary" onClick={loadExplanation} disabled={loading}>
        {loading ? <><span className="spinner" />Generating explanation…</> : error ? "Try again" : "Generate explanation"}
      </button>}
      <p className="ai-disclaimer">AI-generated explanations may be inaccurate. They help interpret detector signals and are not definitive proof of authenticity or manipulation.</p>
    </section>
  );
}
