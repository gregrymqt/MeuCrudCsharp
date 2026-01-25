import React, { useRef, useEffect } from "react";
import styles from "../styles/ClaimChat.module.scss";
import type { FieldValues } from "react-hook-form";
import { type FormField, GenericForm } from "../../../Form/GenericForm";
import { useClaimChatLogic } from "../hooks/useClaimChatLogic";

interface Props {
  claimId: number;
  role: "admin" | "user";
}

// Definindo o formulário de resposta usando a interface do GenericForm [cite: 4]
interface ReplyFormData extends FieldValues {
  message: string;
  attachments: FileList;
}

export const ClaimChat: React.FC<Props> = ({ claimId, role }) => {
  const { messages, handleSendResponse, isLoading } = useClaimChatLogic({
    claimId,
    role,
  });
  const bottomRef = useRef<HTMLDivElement>(null);

  // Auto-scroll para baixo quando chega mensagem nova
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Configuração dos campos para o GenericForm [cite: 4, 8]
  const formFields: FormField<ReplyFormData>[] = [
    {
      name: "message",
      label: "Sua Resposta",
      type: "textarea", // [cite: 3]
      placeholder: "Descreva sua resposta ou solução...",
      validation: { required: "A mensagem é obrigatória" }, // [cite: 6]
      colSpan: 12, // [cite: 7]
    },
    {
      name: "attachments",
      label: "Anexar Comprovantes",
      type: "file", // [cite: 3]
      accept: "image/*, .pdf", // [cite: 23]
      multiple: true, // [cite: 7]
      colSpan: 12, // Ocupa toda a largura [cite: 7]
    },
  ];

  return (
    <div className={styles.container}>
      {/* 1. ÁREA DE MENSAGENS (Timeline) */}
      <div className={styles.timeline}>
        {messages.length === 0 && (
          <p className={styles.empty}>Nenhuma mensagem trocada ainda.</p>
        )}

        {messages.map((msg) => (
          <div
            key={msg.messageId}
            className={`${styles.bubbleWrapper} ${
              msg.isMe ? styles.me : styles.them
            }`}
          >
            <div className={styles.bubble}>
              <div className={styles.header}>
                <span className={styles.role}>
                  {msg.senderRole === "mediator"
                    ? "Mediação MP"
                    : msg.isMe
                    ? "Você"
                    : "Outro lado"}
                </span>
                <span className={styles.date}>
                  {new Date(msg.dateCreated).toLocaleDateString()}
                </span>
              </div>

              <p className={styles.content}>{msg.content}</p>

              {/* Renderização de Anexos recebidos */}
              {msg.attachments && msg.attachments.length > 0 && (
                <div className={styles.attachmentsList}>
                  {msg.attachments.map((att, idx) => (
                    <a
                      key={idx}
                      href={att}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      📎 Anexo {idx + 1}
                    </a>
                  ))}
                </div>
              )}
            </div>
          </div>
        ))}
        <div ref={bottomRef} />
      </div>

      {/* 2. ÁREA DE INPUT (Usando seu GenericForm) */}
      <div className={styles.footer}>
        <GenericForm<ReplyFormData>
          fields={formFields} // [cite: 8]
          onSubmit={handleSendResponse} // [cite: 8]
          submitText={isLoading ? "Enviando..." : "Enviar Resposta"} // [cite: 9]
          isLoading={isLoading} // [cite: 9]
        />
      </div>
    </div>
  );
};
