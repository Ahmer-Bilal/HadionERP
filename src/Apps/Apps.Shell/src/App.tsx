import { useEffect, useState } from "react";
import "./App.css";
import { ShellBar } from "./components/ShellBar";
import { NavigationPane } from "./components/NavigationPane";
import { SystemStatusPage } from "./pages/SystemStatusPage";
import { directionFor, type SupportedLanguageCode } from "./i18n/language";

function App() {
  const [language, setLanguage] = useState<SupportedLanguageCode>("en");
  const direction = directionFor(language);

  useEffect(() => {
    document.documentElement.dir = direction;
    document.documentElement.lang = language;
  }, [direction, language]);

  return (
    <div className="app-shell">
      <ShellBar language={language} onLanguageChange={setLanguage} />
      <div className="app-shell__body">
        <NavigationPane language={language} />
        <main className="app-shell__content">
          <SystemStatusPage language={language} />
        </main>
      </div>
    </div>
  );
}

export default App;
