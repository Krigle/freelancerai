export interface ExtractedJobData {
  title: string;
  company: string;
  skills: string[];
  experienceLevel: string;
  location: string;
  salaryRange: string;
  descriptionSummary: string;
}

export interface Job {
  id: number;
  originalText: string;
  extractedJson: string;
  extracted: ExtractedJobData | null;
  createdAt: string;
}

export interface JobRequestDto {
  jobText: string;
}

