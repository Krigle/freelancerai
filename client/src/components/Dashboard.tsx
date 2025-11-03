import { useEffect, useState } from "react";
import { useJobStore, type SortField } from "../store/useJobStore";
import JobCard from "./JobCard";

export default function Dashboard() {
  const {
    loading,
    error,
    fetchJobs,
    deleteJob,
    clearError,
    filters,
    setFilters,
    clearFilters,
    sortField,
    sortOrder,
    setSortField,
    toggleSortOrder,
    currentPage,
    itemsPerPage,
    setItemsPerPage,
    nextPage,
    previousPage,
    getPaginatedJobs,
    getFilteredJobs,
    getTotalPages,
  } = useJobStore();

  const [searchInput, setSearchInput] = useState(filters.searchQuery);

  const paginatedJobs = getPaginatedJobs();
  const filteredJobs = getFilteredJobs();
  const totalPages = getTotalPages();

  useEffect(() => {
    fetchJobs();
  }, [fetchJobs]);

  // Sync search input with filters
  useEffect(() => {
    setSearchInput(filters.searchQuery);
  }, [filters.searchQuery]);

  const handleJobDelete = async (id: number) => {
    await deleteJob(id);
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setFilters({ searchQuery: searchInput });
  };

  const handleSortChange = (field: SortField) => {
    if (field === sortField) {
      toggleSortOrder();
    } else {
      setSortField(field);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  // Show error as a dismissible notification at the top
  const errorNotification = error && (
    <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 flex items-center justify-between">
      <span>{error}</span>
      <button
        type="button"
        onClick={clearError}
        className="ml-4 text-red-500 hover:text-red-700 font-bold"
      >
        ✕
      </button>
    </div>
  );

  const NoJobsMessage = () => (
    <div>
      {/* Error notification */}
      {errorNotification}

      <div className="text-center py-12">
        <svg
          className="mx-auto h-12 w-12 text-gray-400"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <h3 className="mt-2 text-sm font-medium text-gray-900">
          {filteredJobs.length === 0 && filters.searchQuery
            ? "No jobs match your search"
            : "No jobs analyzed yet"}
        </h3>
        <p className="mt-1 text-sm text-gray-500">
          {filteredJobs.length === 0 && filters.searchQuery
            ? "Try adjusting your search or filters"
            : "Get started by analyzing your first job posting above."}
        </p>
        {filteredJobs.length === 0 && filters.searchQuery && (
          <button
            type="button"
            onClick={clearFilters}
            className="mt-4 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Clear Filters
          </button>
        )}
      </div>
    </div>
  );

  if (filteredJobs.length === 0) {
    return <NoJobsMessage />;
  }

  return (
    <div>
      {/* Error notification */}
      {errorNotification}

      {/* Header with count and refresh */}
      <div className="mb-6 flex justify-between items-center">
        <h2 className="text-2xl font-bold text-gray-900">
          Analyzed Jobs ({filteredJobs.length})
        </h2>
        <button
          type="button"
          onClick={fetchJobs}
          className="px-4 py-2 text-sm bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
        >
          Refresh
        </button>
      </div>

      {/* Search and Filters */}
      <div className="mb-6 space-y-4">
        <form onSubmit={handleSearch} className="flex gap-2">
          <input
            type="text"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search jobs by title, company, skills..."
            className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
          <button
            type="submit"
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Search
          </button>
          {filters.searchQuery && (
            <button
              type="button"
              onClick={clearFilters}
              className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
            >
              Clear
            </button>
          )}
        </form>

        {/* Sort Controls */}
        <div className="flex items-center gap-4 flex-wrap">
          <span className="text-sm font-medium text-gray-700">Sort by:</span>
          {(
            [
              { field: "createdAt", label: "Date" },
              { field: "title", label: "Title" },
              { field: "company", label: "Company" },
              { field: "experienceLevel", label: "Experience" },
            ] as const
          ).map(({ field, label }) => (
            <button
              key={field}
              type="button"
              onClick={() => handleSortChange(field)}
              className={`px-3 py-1 text-sm rounded-lg transition-colors ${
                sortField === field
                  ? "bg-blue-600 text-white"
                  : "bg-gray-100 text-gray-700 hover:bg-gray-200"
              }`}
            >
              {label}
              {sortField === field && (
                <span className="ml-1">{sortOrder === "asc" ? "↑" : "↓"}</span>
              )}
            </button>
          ))}
        </div>

        {/* Items per page */}
        <div className="flex items-center gap-2">
          <span className="text-sm text-gray-700">Show:</span>
          {[10, 20, 50].map((count) => (
            <button
              key={count}
              type="button"
              onClick={() => setItemsPerPage(count)}
              className={`px-3 py-1 text-sm rounded-lg transition-colors ${
                itemsPerPage === count
                  ? "bg-blue-600 text-white"
                  : "bg-gray-100 text-gray-700 hover:bg-gray-200"
              }`}
            >
              {count}
            </button>
          ))}
          <span className="text-sm text-gray-700">per page</span>
        </div>
      </div>

      {/* Job Cards Grid */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3 mb-6 items-start">
        {paginatedJobs.map((job) => (
          <JobCard key={job.id} job={job} onDelete={handleJobDelete} />
        ))}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-center items-center gap-4">
          <button
            type="button"
            onClick={previousPage}
            disabled={currentPage === 1}
            className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Previous
          </button>
          <span className="text-sm text-gray-700">
            Page {currentPage} of {totalPages}
          </span>
          <button
            type="button"
            onClick={nextPage}
            disabled={currentPage === totalPages}
            className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
