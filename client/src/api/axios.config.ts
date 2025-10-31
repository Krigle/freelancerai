import axios, { AxiosError } from "axios";
import axiosRateLimit from "axios-rate-limit";
import type { InternalAxiosRequestConfig, AxiosResponse } from "axios";

const API_BASE_URL =
  import.meta.env.VITE_API_URL || "http://localhost:5000/api";

// Create axios instance with default config
const baseApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
    "User-Agent": "FreelanceFinderAI/1.0",
    Accept: "application/json",
    "Accept-Language": "en-US,en;q=0.9",
    "Cache-Control": "no-cache",
    Pragma: "no-cache",
  },
  timeout: 30000, // 30 seconds
});

// Apply rate limiting: max 10 requests per minute
export const api = axiosRateLimit(baseApi, {
  maxRequests: 10,
  perMilliseconds: 60000, // 1 minute
  maxRPS: 1, // max 1 request per second
});

// Request interceptor
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Log requests in development
    if (import.meta.env.DEV) {
      console.log(
        `[API Request] ${config.method?.toUpperCase()} ${config.url}`
      );
    }
    return config;
  },
  (error: AxiosError) => {
    console.error("[API Request Error]", error);
    return Promise.reject(error);
  }
);

// Response interceptor
api.interceptors.response.use(
  (response: AxiosResponse) => {
    // Log responses in development
    if (import.meta.env.DEV) {
      console.log(
        `[API Response] ${response.config.method?.toUpperCase()} ${
          response.config.url
        }`,
        response.status
      );
    }
    return response;
  },
  (error: AxiosError<{ error?: string }>) => {
    // Enhanced error handling
    if (error.response) {
      // Server responded with error status
      console.error(
        `[API Error] ${error.response.status}:`,
        error.response.data?.error || error.message
      );
    } else if (error.request) {
      // Request made but no response received
      console.error("[API Error] No response received:", error.message);
    } else {
      // Error in request setup
      console.error("[API Error]", error.message);
    }
    return Promise.reject(error);
  }
);

// Error handler helper
export const handleApiError = (error: unknown): never => {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ error?: string }>;

    // Handle specific error cases
    if (axiosError.response) {
      const status = axiosError.response.status;
      const message = axiosError.response.data?.error;

      switch (status) {
        case 400:
          throw new Error(message || "Bad request");
        case 401:
          throw new Error("Unauthorized - Please log in");
        case 403:
          throw new Error("Forbidden - You don't have permission");
        case 404:
          throw new Error(
            message || "Job not found - it may have already been deleted"
          );
        case 500:
          throw new Error(message || "Server error - Please try again later");
        default:
          throw new Error(message || `Error: ${status}`);
      }
    } else if (axiosError.request) {
      throw new Error("Network error - Please check your connection");
    } else {
      throw new Error(axiosError.message || "An error occurred");
    }
  }

  throw new Error("An unexpected error occurred");
};
