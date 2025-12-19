import { Modal } from '../../../Modal/Modal';
import type { ChargebackDetail } from '../types/chargeback.type';
import styles from '../styles/ChargebackDetailModal.module.scss';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  data: ChargebackDetail | null;
  isLoading: boolean;
}

export const ChargebackDetailModal = ({ isOpen, onClose, data, isLoading }: Props) => {
  
  // Conteúdo do Rodapé do Modal
  const modalFooter = (
    <button className={styles.submitBtn} onClick={onClose}>
      Fechar
    </button>
  );

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Detalhes da Contestação"
      size="medium"
      footer={modalFooter}
    >
      {isLoading ? (
        <div className={styles.loadingContainer}>Carregando detalhes...</div>
      ) : data ? (
        <div className={styles.contentWrapper}>
          <div className={styles.detailGrid}>
            <div className={`${styles.infoItem} ${styles.fullWidth}`}>
              <label>ID Mercado Pago</label>
              <span>{data.chargebackId}</span>
            </div>

            <div className={styles.infoItem}>
              <label>Valor</label>
              <span>{data.moeda} {data.valor.toFixed(2)}</span>
            </div>

            <div className={styles.infoItem}>
              <label>Cobertura</label>
              <span className={data.coberturaAplicada ? styles.statusGreen : styles.statusRed}>
                {data.coberturaAplicada ? 'Aplicada' : 'Não Aplicada'}
              </span>
            </div>

            <div className={`${styles.infoItem} ${styles.fullWidth}`}>
              <label>Status Documentação</label>
              <span className={styles.badge}>{data.statusDocumentacao}</span>
            </div>

            <div className={`${styles.infoItem} ${styles.fullWidth}`}>
              <label>Data Limite Disputa</label>
              <span>
                {data.dataLimiteDisputa
                  ? new Date(data.dataLimiteDisputa).toLocaleDateString()
                  : 'N/A'}
              </span>
            </div>
          </div>

          <div className={styles.fileList}>
            <h3>Arquivos Enviados</h3>
            {data.arquivosEnviados.length > 0 ? (
              <ul>
                {data.arquivosEnviados.map((file, idx) => (
                  <li key={idx}>
                    <i className="fas fa-file-pdf" />
                    <a href={file.url} target="_blank" rel="noreferrer">
                      {file.nomeArquivo || `Arquivo ${idx + 1}`} ({file.tipo})
                    </a>
                  </li>
                ))}
              </ul>
            ) : (
              <p className={styles.textMuted}>Nenhum arquivo enviado.</p>
            )}
          </div>
        </div>
      ) : (
        <p>Não foi possível carregar os dados.</p>
      )}
    </Modal>
  );
};