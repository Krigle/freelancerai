import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import JobCard from "./JobCard";
import type { Job } from "../types/index.js";

// Mock window.confirm
const mockConfirm = vi.fn();
global.confirm = mockConfirm;

describe("JobCard", () => {
  const mockOnDelete = vi.fn();

  const mockJob: Job = {
    id: 1,
    text: "Sample job text",
    extracted: {
      title: "Senior React Developer",
      company: "TechCorp",
      skills: ["React", "TypeScript", "Node.js"],
      experienceLevel: "Senior",
      location: "Remote",
      salaryRange: "$100k - $130k",
      descriptionSummary:
        "**Senior React Developer** at **TechCorp**\n\n📍 Remote | 👤 Senior | 💰 $100k - $130k\n\n**About:** We are looking for a Senior React Developer...\n\n**The Role:** Build scalable web applications...\n\n**Requirements:** React expertise, TypeScript proficiency...\n\n**Benefits:** Competitive salary, remote work...",
    },
    createdAt: new Date("2024-01-01T10:00:00Z"),
  };

  const mockJobWithoutExtracted: Job = {
    id: 2,
    text: "Job without extracted data",
    extracted: null,
    createdAt: new Date("2024-01-01T10:00:00Z"),
  };

  beforeEach(() => {
    mockOnDelete.mockClear();
    mockConfirm.mockClear();
  });

  describe("Loading State", () => {
    it("renders loading skeleton when extracted data is null", () => {
      render(<JobCard job={mockJobWithoutExtracted} onDelete={mockOnDelete} />);

      expect(screen.getByText("Generating AI summary...")).toBeInTheDocument();
      expect(screen.getByTestId("loading-spinner")).toBeInTheDocument();
    });

    it("shows loading animation with proper styling", () => {
      render(<JobCard job={mockJobWithoutExtracted} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      expect(card).toHaveClass("animate-pulse");
    });
  });

  describe("Expanded/Collapsed State", () => {
    it("starts in collapsed state by default", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      expect(card).not.toHaveAttribute("aria-expanded", "true");
    });

    it("expands when clicked", async () => {
      const user = userEvent.setup();
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      await user.click(card);

      await waitFor(() => {
        expect(card).toHaveAttribute("aria-expanded", "true");
      });
    });

    it("shows expand/collapse chevron icon", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const chevron = screen.getByTestId("expand-chevron");
      expect(chevron).toBeInTheDocument();
    });

    it("rotates chevron when expanded", async () => {
      const user = userEvent.setup();
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const chevron = screen.getByTestId("expand-chevron");
      expect(chevron).not.toHaveClass("rotate-180");

      const card = screen.getByRole("article");
      await user.click(card);

      await waitFor(() => {
        expect(chevron).toHaveClass("rotate-180");
      });
    });
  });

  describe("Job Information Display", () => {
    it("displays job title and company", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      expect(screen.getByText("Senior React Developer")).toBeInTheDocument();
      expect(screen.getByText("TechCorp")).toBeInTheDocument();
    });

    it("displays location information", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      expect(screen.getByText("📍 Location:")).toBeInTheDocument();
      expect(screen.getByText("Remote")).toBeInTheDocument();
    });

    it("displays experience level", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      expect(screen.getByText("💼 Experience:")).toBeInTheDocument();
      expect(screen.getByText("Senior")).toBeInTheDocument();
    });

    it("displays salary range when available", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      expect(screen.getByText("💰 Salary:")).toBeInTheDocument();
      expect(screen.getByText("$100k - $130k")).toBeInTheDocument();
    });

    it("hides salary section when not available", () => {
      const jobWithoutSalary = { ...mockJob };
      jobWithoutSalary.extracted!.salaryRange = "";

      render(<JobCard job={jobWithoutSalary} onDelete={mockOnDelete} />);

      expect(screen.queryByText("💰 Salary:")).not.toBeInTheDocument();
    });

    it("displays skills as badges", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      expect(screen.getByText("🛠️ Skills:")).toBeInTheDocument();
      expect(screen.getByText("React")).toBeInTheDocument();
      expect(screen.getByText("TypeScript")).toBeInTheDocument();
      expect(screen.getByText("Node.js")).toBeInTheDocument();
    });
  });

  describe("AI Summary Display", () => {
    it("shows AI summary preview in collapsed state", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      expect(screen.getByText("🤖")).toBeInTheDocument();
      expect(screen.getByText("AI Summary")).toBeInTheDocument();
    });

    it("truncates long summary preview", () => {
      const longSummary = "A".repeat(150);
      const jobWithLongSummary = {
        ...mockJob,
        extracted: { ...mockJob.extracted!, descriptionSummary: longSummary },
      };

      render(<JobCard job={jobWithLongSummary} onDelete={mockOnDelete} />);

      const preview = screen.getByText(longSummary.substring(0, 120) + "...");
      expect(preview).toBeInTheDocument();
      expect(screen.getByText("Read more")).toBeInTheDocument();
    });

    it("shows full structured summary when expanded", async () => {
      const user = userEvent.setup();
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      await user.click(card);

      await waitFor(() => {
        expect(screen.getByText("About:")).toBeInTheDocument();
        expect(screen.getByText("The Role:")).toBeInTheDocument();
        expect(screen.getByText("Requirements:")).toBeInTheDocument();
        expect(screen.getByText("Benefits:")).toBeInTheDocument();
      });
    });

    it("renders structured summary with proper formatting", async () => {
      const user = userEvent.setup();
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      await user.click(card);

      await waitFor(() => {
        // Check for markdown-like formatting
        const summaryElement = screen.getByTestId("structured-summary");
        expect(summaryElement).toHaveClass("prose");
      });
    });
  });

  describe("Delete Functionality", () => {
    it("shows delete button", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const deleteButton = screen.getByRole("button", { name: /delete job/i });
      expect(deleteButton).toBeInTheDocument();
    });

    it("calls onDelete when confirmed", async () => {
      const user = userEvent.setup();
      mockConfirm.mockReturnValue(true);

      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const deleteButton = screen.getByRole("button", { name: /delete job/i });
      await user.click(deleteButton);

      expect(mockConfirm).toHaveBeenCalledWith(
        'Are you sure you want to delete "Senior React Developer" from TechCorp? This action cannot be undone.'
      );
      expect(mockOnDelete).toHaveBeenCalledWith(1);
    });

    it("does not call onDelete when cancelled", async () => {
      const user = userEvent.setup();
      mockConfirm.mockReturnValue(false);

      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const deleteButton = screen.getByRole("button", { name: /delete job/i });
      await user.click(deleteButton);

      expect(mockConfirm).toHaveBeenCalled();
      expect(mockOnDelete).not.toHaveBeenCalled();
    });

    it("prevents event bubbling when deleting", async () => {
      const user = userEvent.setup();
      mockConfirm.mockReturnValue(false); // Cancel to avoid expansion

      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const deleteButton = screen.getByRole("button", { name: /delete job/i });
      await user.click(deleteButton);

      // Card should not expand when delete is clicked
      const card = screen.getByRole("article");
      expect(card).not.toHaveAttribute("aria-expanded", "true");
    });
  });

  describe("Metadata Display", () => {
    it("shows job ID and creation date when expanded", async () => {
      const user = userEvent.setup();
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      await user.click(card);

      await waitFor(() => {
        expect(screen.getByText("Job ID:")).toBeInTheDocument();
        expect(screen.getByText("Analyzed on:")).toBeInTheDocument();
      });
    });
  });

  describe("Accessibility", () => {
    it("has proper ARIA labels", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      expect(card).toHaveAttribute(
        "aria-label",
        "Job posting: Senior React Developer at TechCorp"
      );

      const expandButton = screen.getByRole("button", {
        name: /expand job details/i,
      });
      expect(expandButton).toBeInTheDocument();
    });

    it("supports keyboard navigation", async () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      card.focus();

      expect(document.activeElement).toBe(card);

      // Test keyboard expansion
      fireEvent.keyDown(card, { key: "Enter" });

      await waitFor(() => {
        expect(card).toHaveAttribute("aria-expanded", "true");
      });
    });

    it("has proper focus management", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const deleteButton = screen.getByRole("button", { name: /delete job/i });
      expect(deleteButton).toHaveAttribute("tabIndex", "0");
    });
  });

  describe("Edge Cases", () => {
    it("handles missing skills gracefully", () => {
      const jobWithoutSkills = {
        ...mockJob,
        extracted: { ...mockJob.extracted!, skills: [] },
      };

      render(<JobCard job={jobWithoutSkills} onDelete={mockOnDelete} />);

      expect(screen.getByText("🛠️ Skills:")).toBeInTheDocument();
      expect(screen.getByText("See job description")).toBeInTheDocument();
    });

    it("handles null or undefined extracted data fields", () => {
      const jobWithNulls = {
        ...mockJob,
        extracted: {
          ...mockJob.extracted!,
          location: null,
          experienceLevel: undefined,
          salaryRange: "",
        },
      };

      render(<JobCard job={jobWithNulls} onDelete={mockOnDelete} />);

      expect(screen.getByText("Not specified")).toBeInTheDocument();
    });

    it("handles very long job titles with truncation", () => {
      const longTitle = "A".repeat(200);
      const jobWithLongTitle = {
        ...mockJob,
        extracted: { ...mockJob.extracted!, title: longTitle },
      };

      render(<JobCard job={jobWithLongTitle} onDelete={mockOnDelete} />);

      const titleElement = screen.getByRole("heading", { level: 3 });
      expect(titleElement).toHaveClass("truncate");
    });

    it("handles empty summary gracefully", () => {
      const jobWithEmptySummary = {
        ...mockJob,
        extracted: { ...mockJob.extracted!, descriptionSummary: "" },
      };

      render(<JobCard job={jobWithEmptySummary} onDelete={mockOnDelete} />);

      // Should not crash and should not show AI summary section
      expect(screen.queryByText("🤖")).not.toBeInTheDocument();
    });

    it("handles malformed summary without crashing", () => {
      const jobWithMalformedSummary = {
        ...mockJob,
        extracted: {
          ...mockJob.extracted!,
          descriptionSummary: "**Unclosed markdown",
        },
      };

      render(<JobCard job={jobWithMalformedSummary} onDelete={mockOnDelete} />);

      // Should render without crashing
      expect(screen.getByText("🤖")).toBeInTheDocument();
    });
  });

  describe("Responsive Design", () => {
    it("applies responsive classes for different screen sizes", () => {
      render(<JobCard job={mockJob} onDelete={mockOnDelete} />);

      const card = screen.getByRole("article");
      expect(card).toHaveClass("p-4", "sm:p-6"); // Responsive padding

      const title = screen.getByRole("heading", { level: 3 });
      expect(title).toHaveClass("text-xl", "sm:text-2xl"); // Responsive text size
    });
  });
});
