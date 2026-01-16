import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HubConnectionState,
} from "@microsoft/signalr";
import { StorageService, STORAGE_KEYS } from "./storage.service";
import { AppHubs } from "../enums/hub.enums"; // Importe o Enum

class WebSocketService {
  // Agora armazenamos várias conexões: "Payment" -> ConnectionObj, "Video" -> ConnectionObj
  private connections: Map<AppHubs, HubConnection> = new Map();

  /**
   * Inicializa e Conecta a um Hub específico
   */
  public async connect(hubPath: AppHubs): Promise<void> {
    // Se já existe e está conectado, ignora
    if (
      this.connections.has(hubPath) &&
      this.connections.get(hubPath)?.state === HubConnectionState.Connected
    ) {
      return;
    }

    const baseUrl =
      import.meta.env.VITE_GENERAL__BASEURL || "https://localhost:5045";
    const fullUrl = `${baseUrl}${hubPath}`;

    const connection = new HubConnectionBuilder()
      .withUrl(fullUrl, {
        accessTokenFactory: () => {
          const token = StorageService.getItem<string>(STORAGE_KEYS.TOKEN);
          return Promise.resolve(token || "");
        },
        skipNegotiation: true,
        transport: 1,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // Salva no mapa
    this.connections.set(hubPath, connection);

    try {
      await connection.start();
      console.log(`✅ Socket Conectado: ${hubPath}`);
    } catch (err) {
      console.error(`❌ Erro ao conectar em ${hubPath}:`, err);
    }
  }

  /**
   * Desconecta de um Hub específico (ou de todos se não passar nada)
   */
  public disconnect(hubPath?: AppHubs): void {
    if (hubPath) {
      this.connections.get(hubPath)?.stop();
      this.connections.delete(hubPath);
    } else {
      this.connections.forEach((conn) => conn.stop());
      this.connections.clear();
    }
  }

  /**
   * OUVIR (Subscribe) - Agora pede qual Hub você quer ouvir
   */
  public on<T>(
    hubPath: AppHubs,
    methodName: string,
    callback: (data: T) => void
  ): void {
    const connection = this.connections.get(hubPath);
    if (!connection) {
      console.warn(
        `Tentou ouvir evento em ${hubPath} mas não há conexão ativa.`
      );
      return;
    }

    connection.off(methodName); // Evita duplicidade
    connection.on(methodName, callback);
  }

  public off(hubPath: AppHubs, methodName: string): void {
    this.connections.get(hubPath)?.off(methodName);
  }

  /**
   * ENVIAR COMANDO (Invoke)
   * Necessário para chamar o método 'SubscribeToJobProgress' do Backend
   */
  public async invoke<T>(
    hubPath: AppHubs,
    methodName: string,
    ...args: T[]
  ): Promise<void> {
    const connection = this.connections.get(hubPath);

    if (!connection || connection.state !== HubConnectionState.Connected) {
      console.warn(
        `Tentou invocar ${methodName} em ${hubPath}, mas não está conectado. Tentando conectar...`
      );
      await this.connect(hubPath);
    }

    try {
      // Re-obtemos a conexão após tentativa de reconexão
      await this.connections.get(hubPath)?.invoke(methodName, ...args);
    } catch (err) {
      console.error(`❌ Erro ao invocar ${methodName}:`, err);
    }
  }
}

export const socketService = new WebSocketService();
