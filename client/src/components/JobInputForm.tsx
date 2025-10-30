import { useState } from "react";
import { useJobStore } from "../store/useJobStore";

export default function JobInputForm() {
  const [text, setText] = useState("");
  const { analyzing, analyzeError, analyzeJob, clearAnalyzeError } =
    useJobStore();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!text.trim()) {
      return;
    }

    const job = await analyzeJob(text);
    if (job) {
      setText("");
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-4 p-6 bg-white shadow-lg rounded-2xl"
    >
      <div>
        <label
          htmlFor="jobText"
          className="block text-sm font-medium text-gray-700 mb-2"
        >
          Paste Job Listing
        </label>
        <textarea
          id="jobText"
          value={text}
          onChange={(e) => {
            setText(e.target.value);
            if (analyzeError) clearAnalyzeError();
          }}
          placeholder="Paste a job listing here..."
          className="w-full h-48 p-4 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
          disabled={analyzing}
        />
      </div>

      {analyzeError && (
        <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {analyzeError}
        </div>
      )}

      <button
        type="submit"
        disabled={analyzing || !text.trim()}
        className="w-full bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors font-medium"
      >
        {analyzing ? (
          <span className="flex items-center justify-center">
            <svg
              className="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              ></circle>
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              ></path>
            </svg>
            Analyzing...
          </span>
        ) : (
          "Analyze Job Posting"
        )}
      </button>
    </form>
  );
}
