import React, { useState } from "react";
import styles from "../styles/ProfileInfo.module.scss";
import { AvatarUploadForm } from "./AvatarUploadForm"; 
import { Card } from "../../../../components/Card/Card";
import { useAuth } from "../../../auth/hooks/useAuth";

// Removemos a prop 'subscription', pois não é mais responsabilidade deste componente
type TabOption = "details" | "avatar";

export const ProfileInfo: React.FC = () => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<TabOption>("details");

  if (!user) return null;

  return (
    <Card className={styles.card}>
      <div className={styles.tabs}>
        <button
          className={`${styles.tabBtn} ${activeTab === "details" ? styles.active : ""}`}
          onClick={() => setActiveTab("details")}
        >
          <i className="fas fa-id-card me-2"></i> Dados
        </button>
        <button
          className={`${styles.tabBtn} ${activeTab === "avatar" ? styles.active : ""}`}
          onClick={() => setActiveTab("avatar")}
        >
          <i className="fas fa-camera me-2"></i> Foto
        </button>
      </div>

      <Card.Body title="">
        {/* ABA: DADOS */}
        {activeTab === "details" && (
          <div className="text-center fade-in">
            <img
              src={user.avatarUrl || "/default-user.png"}
              alt={`Foto de ${user.name}`}
              className={styles.avatar}
            />

            <h2 className="mt-3">{user.name}</h2>
            <p className="text-muted">{user.email}</p>
            
            {/* Removemos badges de plano e botões de upgrade */}
          </div>
        )}

        {/* ABA: FOTO */}
        {activeTab === "avatar" && (
          <div className="fade-in">
            <div className="text-center mb-3">
              <img
                src={user.avatarUrl || "/default-user.png"}
                alt="Preview"
                className={`${styles.avatar} ${styles.avatarSmall}`}
              />
            </div>
            <AvatarUploadForm />
          </div>
        )}
      </Card.Body>
    </Card>
  );
};