import { Link } from 'react-router-dom';
import '../styles/Footer.scss';

export const Footer = () => {
  const year = new Date().getFullYear();

  return (
    <footer className="site-footer">
      <div className="container footer-grid">
        {/* About */}
        <div className="footer-about">
          <h3 className="footer-logo">
            <i className="fas fa-graduation-cap"></i>
            <span>SeuCurso</span>
          </h3>
          <p>Transformando conhecimento em sucesso. A melhor plataforma para você aprender e crescer profissionalmente.</p>
        </div>

        {/* Links Navegação */}
        <div className="footer-links">
          <h4>Navegação</h4>
          <ul>
            <li><Link to="/cursos">Cursos</Link></li>
            <li><Link to="/sobre">Sobre Nós</Link></li>
            <li><Link to="/contato">Contato</Link></li>
            <li><Link to="/faq">FAQ</Link></li>
          </ul>
        </div>

        {/* Links Legal */}
        <div className="footer-links">
          <h4>Legal</h4>
          <ul>
            <li><Link to="/privacidade">Política de Privacidade</Link></li>
            <li><Link to="/termos">Termos de Uso</Link></li>
          </ul>
        </div>

        {/* Social */}
        <div className="footer-social">
          <h4>Siga-nos</h4>
          <div className="social-icons">
            <a href="#" aria-label="Facebook"><i className="fab fa-facebook-f"></i></a>
            <a href="#" aria-label="Instagram"><i className="fab fa-instagram"></i></a>
            <a href="#" aria-label="LinkedIn"><i className="fab fa-linkedin-in"></i></a>
            <a href="#" aria-label="YouTube"><i className="fab fa-youtube"></i></a>
          </div>
        </div>
      </div>

      <div className="footer-bottom">
        <div className="container">
          &copy; {year} - SeuCurso. Todos os direitos reservados.
        </div>
      </div>
    </footer>
  );
};