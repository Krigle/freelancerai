import type { Job, JobRequestDto } from "../types/index.js";
import { api, handleApiError } from "./axios.config";

// Alias for consistency
const handleError = handleApiError;

export async function analyzeJob(jobText: string): Promise<Job> {
  try {
    const response = await api.post<Job>("/jobs/analyze", {
      jobText,
    } as JobRequestDto);
    return response.data;
  } catch (error) {
    return handleError(error);
  }
}

export async function getJobs(): Promise<Job[]> {
  try {
    const response = await api.get<Job[]>("/jobs");
    return response.data;
  } catch (error) {
    return handleError(error);
  }
}

export async function getJob(id: number): Promise<Job> {
  try {
    const response = await api.get<Job>(`/jobs/${id}`);
    return response.data;
  } catch (error) {
    return handleError(error);
  }
}

export async function deleteJob(id: number): Promise<void> {
  try {
    await api.delete(`/jobs/${id}`);
  } catch (error) {
    return handleError(error);
  }
}
