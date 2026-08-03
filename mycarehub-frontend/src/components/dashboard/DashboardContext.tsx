"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type { ProfiloDto } from "@/src/types/dto";

type DashboardContextValue = {
  profilo: ProfiloDto;
  email: string | null;
  refreshProfilo: () => Promise<void>;
  setProfilo: (profilo: ProfiloDto) => void;
};

const DashboardContext = createContext<DashboardContextValue | null>(null);

export function DashboardProvider({
  profilo: initial,
  email,
  onRefresh,
  children,
}: {
  profilo: ProfiloDto;
  email: string | null;
  onRefresh: () => Promise<ProfiloDto>;
  children: ReactNode;
}) {
  const [profilo, setProfilo] = useState(initial);

  const refreshProfilo = useCallback(async () => {
    const next = await onRefresh();
    setProfilo(next);
  }, [onRefresh]);

  const value = useMemo(
    () => ({ profilo, email, refreshProfilo, setProfilo }),
    [profilo, email, refreshProfilo]
  );

  return (
    <DashboardContext.Provider value={value}>
      {children}
    </DashboardContext.Provider>
  );
}

export function useDashboard(): DashboardContextValue {
  const ctx = useContext(DashboardContext);
  if (!ctx) {
    throw new Error("useDashboard deve essere usato dentro DashboardProvider");
  }
  return ctx;
}
