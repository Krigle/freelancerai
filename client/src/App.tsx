import JobInputForm from "./components/JobInputForm";
import Dashboard from "./components/Dashboard";

function App() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <header className="text-center mb-12">
          <h1 className="text-5xl font-bold text-gray-900 mb-3">
            🧭 FreelanceFinderAI
          </h1>
          <p className="text-xl text-gray-600">
            AI-Powered Job Listing Analyzer
          </p>
          <p className="text-sm text-gray-500 mt-2">
            Paste any job listing and get structured insights instantly
          </p>
        </header>

        {/* Main Content */}
        <div className="max-w-4xl mx-auto mb-12">
          <JobInputForm />
        </div>

        {/* Dashboard */}
        <div className="max-w-7xl mx-auto">
          <Dashboard />
        </div>

        {/* Footer */}
        <footer className="text-center mt-16 text-gray-500 text-sm">
          <p>Built with C# (.NET 9) + React + TypeScript + Zustand + OpenAI</p>
        </footer>
      </div>
    </div>
  );
}

export default App;
