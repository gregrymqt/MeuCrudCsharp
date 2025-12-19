import React, { useState } from 'react';
import styles from '../PixPayment.module.scss';
import type { PixResponse } from '../types/pix.types';

interface PixQRCodeProps {
  data: PixResponse;
  onCopy: () => Promise<boolean>;
}

export const PixQRCode: React.FC<PixQRCodeProps> = ({ data, onCopy }) => {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    const success = await onCopy();
    if (success) {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000); // Reset após 2s
    }
  };

  return (
    <div className={styles.qrWrapper}>
      <h3>Pague para finalizar</h3>
      <p className={styles.subtitle}>Escaneie o QR Code abaixo com o app do seu banco</p>
      
      <img 
        src={`data:image/jpeg;base64,${data.qrCodeBase64}`} 
        alt="QR Code PIX" 
        className={styles.qrImage}
      />

      <div className={styles.copyPaste}>
        <input type="text" value={data.qrCode} readOnly />
        <button onClick={handleCopy} title="Copiar código">
          <i className={`fas ${copied ? 'fa-check' : 'fa-copy'}`}></i>
          {copied ? ' Copiado!' : ' Copiar'}
        </button>
      </div>

      <p className={styles.expirationInfo}>
        <i className="fas fa-info-circle"></i> Este código expira em 30 minutos.
      </p>
    </div>
  );
};