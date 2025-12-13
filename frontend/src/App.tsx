import { BrowserRouter } from 'react-router-dom';
import { AppRoutes } from './routes/AppRoutes';
import './App.css';

function App() {
  return (
    // O BrowserRouter deve envolver toda a aplicação
    <BrowserRouter>
      <div className="app-container">
        {/* Aqui é onde as páginas mágicamente trocam de lugar */}
        <AppRoutes />
      </div>
    </BrowserRouter>
  );
}

export default App;