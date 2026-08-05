import { api } from "@/src/lib/api";
import type {
  ChatContattoDto,
  ConversazioneCreateDto,
  ConversazioneDto,
  MessaggioCreateDto,
  MessaggioDto,
} from "@/src/types/dto";

export async function fetchConversazioniApi(): Promise<ConversazioneDto[]> {
  return api.get<ConversazioneDto[]>("/api/Chat/conversazioni");
}

export async function fetchChatContattiApi(): Promise<ChatContattoDto[]> {
  return api.get<ChatContattoDto[]>("/api/Chat/contatti");
}

export async function apriConversazioneApi(
  contattoId: string
): Promise<ConversazioneDto> {
  const payload: ConversazioneCreateDto = { contattoId };
  return api.post<ConversazioneDto>("/api/Chat/conversazioni", payload);
}

export async function fetchMessaggiApi(
  conversazioneId: string,
  after?: string | null
): Promise<MessaggioDto[]> {
  const qs = after ? `?after=${encodeURIComponent(after)}` : "";
  return api.get<MessaggioDto[]>(
    `/api/Chat/conversazioni/${conversazioneId}/messaggi${qs}`
  );
}

export async function inviaMessaggioApi(
  conversazioneId: string,
  contenuto: string
): Promise<MessaggioDto> {
  const payload: MessaggioCreateDto = { contenuto };
  return api.post<MessaggioDto>(
    `/api/Chat/conversazioni/${conversazioneId}/messaggi`,
    payload
  );
}

export async function segnaMessaggiLettiApi(
  conversazioneId: string
): Promise<void> {
  await api.post(`/api/Chat/conversazioni/${conversazioneId}/letti`);
}
