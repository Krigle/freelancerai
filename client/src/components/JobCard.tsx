import { useState } from "react";
import type { Job } from "../types/index.js";

interface JobCardProps {
  job: Job;
  onDelete: (id: number) => void;
}

export default function JobCard({ job, onDelete }: JobCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent card expansion when clicking delete
    onDelete(job.id);
  };

  const toggleExpand = (e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent event bubbling
    setIsExpanded((prev) => !prev);
  };

  const extracted = job.extracted;

  // Loading state when extracted data is not yet available
  if (!extracted) {
    return (
      <div className="bg-white shadow-lg rounded-2xl p-4 sm:p-6 animate-pulse">
        <div className="flex justify-between items-start mb-4">
          <div className="flex-1 min-w-0">
            <div className="h-6 bg-gray-300 rounded mb-2"></div>
            <div className="h-4 bg-gray-300 rounded"></div>
          </div>
          <div className="h-5 w-5 bg-gray-300 rounded"></div>
        </div>
        <div className="space-y-2">
          <div className="h-4 bg-gray-300 rounded"></div>
          <div className="h-4 bg-gray-300 rounded"></div>
          <div className="h-4 bg-gray-300 rounded"></div>
        </div>
        <div className="mt-4 flex items-center gap-2">
          <div className="h-4 w-4 bg-blue-300 rounded-full animate-spin"></div>
          <span className="text-sm text-gray-500">
            Generating AI summary...
          </span>
        </div>
      </div>
    );
  }

  // Function to parse and render structured summary
  const renderStructuredSummary = (summary: string) => {
    const sections = summary.split(
      /\n(?=\*\*|\b(?:Overview|Responsibilities|Requirements|Benefits|Company|Role)\b:)/i
    );
    return sections.map((section, index) => {
      const trimmed = section.trim();
      if (!trimmed) return null;

      // Check if it's a section header
      const headerMatch = trimmed.match(
        /^(\*\*)?([A-Za-z\s]+):(\*\*)?\s*(.*)$/
      );
      if (headerMatch) {
        const [, , header, , content] = headerMatch;
        return (
          <div key={index} className="mb-4">
            <h5 className="font-semibold text-gray-900 text-sm sm:text-base mb-2 capitalize">
              {header.trim()}
            </h5>
            <p className="text-gray-700 leading-relaxed text-sm sm:text-base">
              {content.trim()}
            </p>
          </div>
        );
      }

      // Regular paragraph
      return (
        <p
          key={index}
          className="text-gray-700 leading-relaxed text-sm sm:text-base mb-3"
        >
          {trimmed}
        </p>
      );
    });
  };

  // Summary preview for compact view
  const summaryPreview = extracted.descriptionSummary
    ? extracted.descriptionSummary.length > 120
      ? extracted.descriptionSummary.substring(0, 120) + "..."
      : extracted.descriptionSummary
    : null;

  return (
    <div
      className={`bg-white shadow-lg rounded-2xl p-4 sm:p-6 hover:shadow-xl hover:scale-[1.02] transition-all duration-300 focus-within:ring-2 focus-within:ring-blue-400 ${
        isExpanded ? "ring-2 ring-blue-500 shadow-2xl scale-[1.01]" : ""
      }`}
      role="article"
      aria-expanded={isExpanded}
      aria-label={`Job posting: ${extracted.title} at ${extracted.company}`}
    >
      <div
        onClick={toggleExpand}
        onKeyDown={(e: React.KeyboardEvent) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            toggleExpand(e as unknown as React.MouseEvent);
          }
        }}
        className="flex justify-between items-start mb-4 cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-400 rounded-lg p-2 -m-2"
        tabIndex={0}
        role="button"
        aria-label={isExpanded ? "Collapse job details" : "Expand job details"}
      >
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <h3 className="text-xl sm:text-2xl font-bold text-gray-900 mb-1 truncate bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
              {extracted.title}
            </h3>
            <svg
              xmlns="http://www.w3.org/2000/svg"
              className={`h-5 w-5 text-gray-400 transition-transform duration-300 flex-shrink-0 ${
                isExpanded ? "rotate-180" : ""
              }`}
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M19 9l-7 7-7-7"
              />
            </svg>
          </div>
          <p className="text-base sm:text-lg text-gray-600 truncate">
            {extracted.company}
          </p>
        </div>
        <button
          type="button"
          onClick={handleDelete}
          onKeyDown={(e: React.KeyboardEvent) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              handleDelete(e as unknown as React.MouseEvent);
            }
          }}
          className="text-red-500 hover:text-red-700 p-2 rounded-lg hover:bg-red-50 transition-colors flex-shrink-0 focus:outline-none focus:ring-2 focus:ring-red-400"
          title="Delete job"
          aria-label="Delete job"
          tabIndex={0}
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-5 w-5"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path
              fillRule="evenodd"
              d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z"
              clipRule="evenodd"
            />
          </svg>
        </button>
      </div>

      {/* Compact View - Always Visible */}
      <div className="space-y-2 sm:space-y-3 mt-4">
        <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-2 text-sm">
          <span className="font-semibold text-gray-700">📍 Location:</span>
          <span className="text-gray-600 truncate">
            {extracted.location || "Not specified"}
          </span>
        </div>

        <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-2 text-sm">
          <span className="font-semibold text-gray-700">💼 Experience:</span>
          <span className="text-gray-600">
            {extracted.experienceLevel || "Not specified"}
          </span>
        </div>

        {extracted.salaryRange && (
          <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-2 text-sm">
            <span className="font-semibold text-gray-700">💰 Salary:</span>
            <span className="text-gray-600">{extracted.salaryRange}</span>
          </div>
        )}

        {extracted.skills && extracted.skills.length > 0 && (
          <div className="mt-4">
            <span className="font-semibold text-gray-700 text-sm block mb-3">
              🛠️ Skills:
            </span>
            <div className="flex flex-wrap gap-1 sm:gap-2">
              {extracted.skills.map((skill, index) => (
                <span
                  key={index}
                  className="px-2 sm:px-3 py-1 bg-gradient-to-r from-blue-100 to-indigo-100 text-blue-800 rounded-full text-xs sm:text-sm font-medium shadow-sm hover:shadow-md transition-shadow"
                >
                  {skill}
                </span>
              ))}
            </div>
          </div>
        )}

        {/* AI Summary Preview in Compact View */}
        {summaryPreview && (
          <div className="mt-4">
            <h4 className="font-semibold text-gray-900 text-sm mb-2 flex items-center gap-2">
              <span className="text-lg">🤖</span>
              <span>AI Summary</span>
            </h4>
            <div className="bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 rounded-lg p-3 border border-blue-200">
              <p className="text-gray-700 leading-relaxed text-sm">
                {summaryPreview}
                {extracted.descriptionSummary.length > 120 && (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setIsExpanded(true);
                    }}
                    className="text-blue-600 hover:text-blue-800 font-medium ml-1 focus:outline-none focus:ring-2 focus:ring-blue-400 rounded"
                  >
                    Read more
                  </button>
                )}
              </p>
            </div>
          </div>
        )}
      </div>

      {/* Expanded View - Only When Expanded */}
      {isExpanded && (
        <div className="mt-6 sm:mt-8 pt-4 sm:pt-6 border-t border-gray-200 space-y-6 animate-fadeIn">
          {/* AI-Generated Summary */}
          {extracted.descriptionSummary && (
            <div>
              <h4 className="font-semibold text-gray-900 text-base sm:text-lg mb-3 flex items-center gap-2">
                <span className="text-xl sm:text-2xl">🤖</span>
                <span>AI Summary</span>
              </h4>
              <div className="bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 rounded-lg p-3 sm:p-5 border border-blue-200 shadow-inner max-h-96 overflow-y-auto">
                <div className="prose prose-sm max-w-none">
                  {renderStructuredSummary(extracted.descriptionSummary)}
                </div>
              </div>
            </div>
          )}

          {/* Metadata */}
          <div className="bg-gradient-to-r from-gray-50 to-slate-50 rounded-lg p-3 sm:p-4 border border-gray-200">
            <h4 className="font-semibold text-gray-900 text-sm mb-2 flex items-center gap-2">
              <span>ℹ️</span>
              <span>Metadata</span>
            </h4>
            <div className="space-y-1 text-xs text-gray-600">
              <p>
                <span className="font-semibold">Job ID:</span> {job.id}
              </p>
              <p>
                <span className="font-semibold">Analyzed on:</span>{" "}
                {new Date(job.createdAt).toLocaleDateString()} at{" "}
                {new Date(job.createdAt).toLocaleTimeString()}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Footer - Always Visible */}
      {!isExpanded && (
        <div className="mt-3 sm:mt-4 pt-3 sm:pt-4 border-t border-gray-200">
          <span className="text-xs text-gray-400 block sm:inline">
            Click to expand • Analyzed on{" "}
            {new Date(job.createdAt).toLocaleDateString()}
          </span>
        </div>
      )}
    </div>
  );
}
