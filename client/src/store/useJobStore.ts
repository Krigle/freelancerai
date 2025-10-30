import { create } from "zustand";
import { devtools, persist } from "zustand/middleware";
import type { Job } from "../types/index.js";
import {
  analyzeJob as analyzeJobApi,
  getJobs as getJobsApi,
  deleteJob as deleteJobApi,
} from "../api/jobs";

export type SortField = "createdAt" | "title" | "company" | "experienceLevel";
export type SortOrder = "asc" | "desc";

export interface JobFilters {
  searchQuery: string;
  experienceLevel: string;
  location: string;
  skills: string[];
}

interface JobState {
  // State
  jobs: Job[];
  loading: boolean;
  error: string | null;
  analyzing: boolean;
  analyzeError: string | null;

  // Filtering
  filters: JobFilters;

  // Sorting
  sortField: SortField;
  sortOrder: SortOrder;

  // Pagination
  currentPage: number;
  itemsPerPage: number;

  // Actions
  fetchJobs: () => Promise<void>;
  analyzeJob: (jobText: string) => Promise<Job | null>;
  deleteJob: (id: number) => Promise<void>;
  clearError: () => void;
  clearAnalyzeError: () => void;

  // Filter actions
  setFilters: (filters: Partial<JobFilters>) => void;
  clearFilters: () => void;

  // Sort actions
  setSortField: (field: SortField) => void;
  setSortOrder: (order: SortOrder) => void;
  toggleSortOrder: () => void;

  // Pagination actions
  setCurrentPage: (page: number) => void;
  setItemsPerPage: (items: number) => void;
  nextPage: () => void;
  previousPage: () => void;

  // Computed getters
  getFilteredJobs: () => Job[];
  getSortedJobs: () => Job[];
  getPaginatedJobs: () => Job[];
  getTotalPages: () => number;
}

const defaultFilters: JobFilters = {
  searchQuery: "",
  experienceLevel: "",
  location: "",
  skills: [],
};

export const useJobStore = create<JobState>()(
  persist(
    devtools(
      (set, get) => ({
        // Initial state
        jobs: [],
        loading: false,
        error: null,
        analyzing: false,
        analyzeError: null,

        // Filter state
        filters: defaultFilters,

        // Sort state
        sortField: "createdAt",
        sortOrder: "desc",

        // Pagination state
        currentPage: 1,
        itemsPerPage: 10,

        // Fetch all jobs
        fetchJobs: async () => {
          set({ loading: true, error: null });
          try {
            const jobs = await getJobsApi();
            set({ jobs, loading: false });
          } catch (err) {
            set({
              error: err instanceof Error ? err.message : "Failed to load jobs",
              loading: false,
            });
          }
        },

        // Analyze a new job with optimistic update
        analyzeJob: async (jobText: string) => {
          set({ analyzing: true, analyzeError: null });

          // Optimistic update: create temporary job
          const tempId = Date.now();
          const optimisticJob: Job = {
            id: tempId,
            originalText: jobText,
            extractedJson: "",
            extracted: null,
            createdAt: new Date().toISOString(),
          };

          // Add optimistic job to the list
          set((state) => ({
            jobs: [optimisticJob, ...state.jobs],
          }));

          try {
            const job = await analyzeJobApi(jobText);

            // Replace optimistic job with real job
            set((state) => ({
              jobs: [job, ...state.jobs.filter((j) => j.id !== tempId)],
              analyzing: false,
            }));
            return job;
          } catch (err) {
            // Remove optimistic job on error
            set((state) => ({
              jobs: state.jobs.filter((j) => j.id !== tempId),
              analyzeError:
                err instanceof Error ? err.message : "Failed to analyze job",
              analyzing: false,
            }));
            return null;
          }
        },

        // Delete a job with optimistic update
        deleteJob: async (id: number) => {
          // Optimistic update: remove job immediately
          const previousJobs = get().jobs;
          set((state) => ({
            jobs: state.jobs.filter((job) => job.id !== id),
          }));

          try {
            await deleteJobApi(id);
          } catch (err) {
            // Rollback on error
            set({
              jobs: previousJobs,
              error:
                err instanceof Error ? err.message : "Failed to delete job",
            });
          }
        },

        // Clear error
        clearError: () => set({ error: null }),

        // Clear analyze error
        clearAnalyzeError: () => set({ analyzeError: null }),

        // Filter actions
        setFilters: (newFilters: Partial<JobFilters>) =>
          set((state) => ({
            filters: { ...state.filters, ...newFilters },
            currentPage: 1, // Reset to first page when filtering
          })),

        clearFilters: () =>
          set({
            filters: defaultFilters,
            currentPage: 1,
          }),

        // Sort actions
        setSortField: (field: SortField) =>
          set({
            sortField: field,
            currentPage: 1,
          }),

        setSortOrder: (order: SortOrder) =>
          set({
            sortOrder: order,
            currentPage: 1,
          }),

        toggleSortOrder: () =>
          set((state) => ({
            sortOrder: state.sortOrder === "asc" ? "desc" : "asc",
            currentPage: 1,
          })),

        // Pagination actions
        setCurrentPage: (page: number) => set({ currentPage: page }),

        setItemsPerPage: (items: number) =>
          set({
            itemsPerPage: items,
            currentPage: 1,
          }),

        nextPage: () =>
          set((state) => {
            const totalPages = Math.ceil(
              get().getFilteredJobs().length / state.itemsPerPage
            );
            return {
              currentPage: Math.min(state.currentPage + 1, totalPages),
            };
          }),

        previousPage: () =>
          set((state) => ({
            currentPage: Math.max(state.currentPage - 1, 1),
          })),

        // Computed getters
        getFilteredJobs: () => {
          const state = get();
          let filtered = [...state.jobs];

          // Apply search query
          if (state.filters.searchQuery) {
            const query = state.filters.searchQuery.toLowerCase();
            filtered = filtered.filter((job) => {
              const extracted = job.extracted;
              return (
                job.originalText.toLowerCase().includes(query) ||
                extracted?.title?.toLowerCase().includes(query) ||
                extracted?.company?.toLowerCase().includes(query) ||
                extracted?.descriptionSummary?.toLowerCase().includes(query) ||
                extracted?.skills?.some((skill) =>
                  skill.toLowerCase().includes(query)
                )
              );
            });
          }

          // Apply experience level filter
          if (state.filters.experienceLevel) {
            filtered = filtered.filter(
              (job) =>
                job.extracted?.experienceLevel === state.filters.experienceLevel
            );
          }

          // Apply location filter
          if (state.filters.location) {
            const location = state.filters.location.toLowerCase();
            filtered = filtered.filter((job) =>
              job.extracted?.location?.toLowerCase().includes(location)
            );
          }

          // Apply skills filter
          if (state.filters.skills.length > 0) {
            filtered = filtered.filter((job) =>
              state.filters.skills.some((skill) =>
                job.extracted?.skills?.some((jobSkill) =>
                  jobSkill.toLowerCase().includes(skill.toLowerCase())
                )
              )
            );
          }

          return filtered;
        },

        getSortedJobs: () => {
          const state = get();
          const filtered = state.getFilteredJobs();
          const sorted = [...filtered];

          sorted.sort((a, b) => {
            let aValue: string | number;
            let bValue: string | number;

            switch (state.sortField) {
              case "createdAt":
                aValue = new Date(a.createdAt).getTime();
                bValue = new Date(b.createdAt).getTime();
                break;
              case "title":
                aValue = a.extracted?.title || "";
                bValue = b.extracted?.title || "";
                break;
              case "company":
                aValue = a.extracted?.company || "";
                bValue = b.extracted?.company || "";
                break;
              case "experienceLevel":
                aValue = a.extracted?.experienceLevel || "";
                bValue = b.extracted?.experienceLevel || "";
                break;
              default:
                return 0;
            }

            if (aValue < bValue) return state.sortOrder === "asc" ? -1 : 1;
            if (aValue > bValue) return state.sortOrder === "asc" ? 1 : -1;
            return 0;
          });

          return sorted;
        },

        getPaginatedJobs: () => {
          const state = get();
          const sorted = state.getSortedJobs();
          const start = (state.currentPage - 1) * state.itemsPerPage;
          const end = start + state.itemsPerPage;
          return sorted.slice(start, end);
        },

        getTotalPages: () => {
          const state = get();
          const filtered = state.getFilteredJobs();
          return Math.ceil(filtered.length / state.itemsPerPage);
        },
      }),
      {
        name: "job-store",
      }
    ),
    {
      name: "freelance-finder-jobs",
      partialize: (state) => ({
        jobs: state.jobs,
        filters: state.filters,
        sortField: state.sortField,
        sortOrder: state.sortOrder,
        itemsPerPage: state.itemsPerPage,
      }),
    }
  )
);
