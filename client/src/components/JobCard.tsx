import type { Job } from "../types/index.js";
import { deleteJob } from "../api/jobs";

interface JobCardProps {
  job: Job;
  onDelete: (id: number) => void;
}

export default function JobCard({ job, onDelete }: JobCardProps) {
  const handleDelete = async () => {
    if (window.confirm("Are you sure you want to delete this job?")) {
      try {
        await deleteJob(job.id);
        onDelete(job.id);
      } catch (error) {
        console.error("Failed to delete job:", error);
        alert("Failed to delete job");
      }
    }
  };

  const extracted = job.extracted;

  if (!extracted) {
    return null;
  }

  return (
    <div className="bg-white shadow-lg rounded-2xl p-6 hover:shadow-xl transition-shadow">
      <div className="flex justify-between items-start mb-4">
        <div className="flex-1">
          <h3 className="text-2xl font-bold text-gray-900 mb-1">
            {extracted.title}
          </h3>
          <p className="text-lg text-gray-600">{extracted.company}</p>
        </div>
        <button
          onClick={handleDelete}
          className="text-red-500 hover:text-red-700 p-2 rounded-lg hover:bg-red-50 transition-colors"
          title="Delete job"
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

      <div className="space-y-3">
        <div className="flex items-center gap-2 text-sm">
          <span className="font-semibold text-gray-700">📍 Location:</span>
          <span className="text-gray-600">
            {extracted.location || "Not specified"}
          </span>
        </div>

        <div className="flex items-center gap-2 text-sm">
          <span className="font-semibold text-gray-700">💼 Experience:</span>
          <span className="text-gray-600">
            {extracted.experienceLevel || "Not specified"}
          </span>
        </div>

        {extracted.salaryRange && (
          <div className="flex items-center gap-2 text-sm">
            <span className="font-semibold text-gray-700">💰 Salary:</span>
            <span className="text-gray-600">{extracted.salaryRange}</span>
          </div>
        )}

        {extracted.skills && extracted.skills.length > 0 && (
          <div className="mt-3">
            <span className="font-semibold text-gray-700 text-sm block mb-2">
              🛠️ Skills:
            </span>
            <div className="flex flex-wrap gap-2">
              {extracted.skills.map((skill, index) => (
                <span
                  key={index}
                  className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm font-medium"
                >
                  {skill}
                </span>
              ))}
            </div>
          </div>
        )}

        {extracted.descriptionSummary && (
          <div className="mt-4 pt-4 border-t border-gray-200">
            <span className="font-semibold text-gray-700 text-sm block mb-2">
              📝 Summary:
            </span>
            <p className="text-gray-600 text-sm leading-relaxed">
              {extracted.descriptionSummary}
            </p>
          </div>
        )}

        <div className="mt-4 pt-4 border-t border-gray-200">
          <span className="text-xs text-gray-400">
            Analyzed on {new Date(job.createdAt).toLocaleDateString()} at{" "}
            {new Date(job.createdAt).toLocaleTimeString()}
          </span>
        </div>
      </div>
    </div>
  );
}
