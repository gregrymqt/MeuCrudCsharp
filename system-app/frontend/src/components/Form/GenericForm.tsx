import { type ReactNode } from 'react';
import  { 
   useForm, 
  type SubmitHandler, 
  type FieldValues, 
  type Path, 
  type RegisterOptions, 
  type DefaultValues, 
  type FieldError 
} from 'react-hook-form';
import './GenericForm.scss';

// 1. ADICIONADO 'file' NOS TIPOS ACEITOS
export type InputType = 
  | 'text' | 'email' | 'password' | 'number' | 'date' 
  | 'textarea' | 'select' | 'checkbox' | 'file';

// 2. CORREÇÃO NA INTERFACE (Adicionado extends FieldValues para garantir a tipagem)
export interface FormField<T extends FieldValues> {
  name: Path<T>;
  label: string;
  type: InputType;
  placeholder?: string;
  options?: { label: string; value: string | number }[];
  // CORREÇÃO: Tipagem estrita da validação ligada ao T
  validation?: RegisterOptions<T, Path<T>>; 
  colSpan?: 3 | 4 | 6 | 12;
  disabled?: boolean;
  // Novo: aceita multiplos arquivos ou formatos específicos
  accept?: string; 
  multiple?: boolean;
}

interface GenericFormProps<T extends FieldValues> {
  fields: FormField<T>[];
  onSubmit: SubmitHandler<T>;
  // CORREÇÃO: O tipo correto para defaultValues no hook-form é DefaultValues<T>
  defaultValues?: DefaultValues<T>; 
  submitText?: string;
  isLoading?: boolean;
  children?: ReactNode;
}

export const GenericForm = <T extends FieldValues>({ 
  fields, 
  onSubmit, 
  defaultValues, 
  submitText = "Salvar",
  isLoading = false,
  children 
}: GenericFormProps<T>) => {

  const { 
    register, 
    handleSubmit, 
    formState: { errors } 
  } = useForm<T>({ defaultValues });

  return (
    <form className="generic-form" onSubmit={handleSubmit(onSubmit)}>
      <div className="generic-form__grid">
        
        {fields.map((field) => {
          // CORREÇÃO: Type Casting seguro para FieldError em vez de 'any'
          const fieldError = errors[field.name] as FieldError | undefined;
          const errorMessage = fieldError?.message;
          
          const isCheckbox = field.type === 'checkbox';
          const colClass = field.colSpan ? `col-span-${field.colSpan}` : 'col-span-12';

          return (
            <div 
              key={String(field.name)} 
              className={`form-field ${colClass} ${isCheckbox ? 'checkbox-field' : ''}`}
            >
              {/* Rótulo (Label) */}
              {!isCheckbox && (
                <label htmlFor={String(field.name)}>
                  {field.label}
                  {field.validation?.required && <span className="required">*</span>}
                </label>
              )}

              {/* --- RENDERIZAÇÃO DOS INPUTS --- */}
              
              {field.type === 'textarea' ? (
                <textarea
                  id={String(field.name)}
                  placeholder={field.placeholder}
                  disabled={field.disabled}
                  className={errorMessage ? 'error' : ''}
                  {...register(field.name, field.validation)}
                />
              ) : field.type === 'select' ? (
                <select
                  id={String(field.name)}
                  disabled={field.disabled}
                  className={errorMessage ? 'error' : ''}
                  {...register(field.name, field.validation)}
                >
                  <option value="">Selecione...</option>
                  {field.options?.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              ) : field.type === 'checkbox' ? (
                <>
                   <input
                    type="checkbox"
                    id={String(field.name)}
                    disabled={field.disabled}
                    {...register(field.name, field.validation)}
                  />
                  <label htmlFor={String(field.name)}>{field.label}</label>
                </>
              ) : field.type === 'file' ? (
                // 3. ADICIONADO LÓGICA PARA ARQUIVO
                <input 
                  id={String(field.name)}
                  type="file"
                  disabled={field.disabled}
                  accept={field.accept} // ex: "image/*, .pdf"
                  multiple={field.multiple}
                  className={errorMessage ? 'error' : ''}
                  // Nota: file input no React Hook Form usa FileList
                  {...register(field.name, field.validation)}
                />
              ) : (
                // Inputs Padrões (text, password, date, number, etc)
                <input
                  id={String(field.name)}
                  type={field.type}
                  placeholder={field.placeholder}
                  disabled={field.disabled}
                  className={errorMessage ? 'error' : ''}
                  {...register(field.name, field.validation)}
                />
              )}

              {/* Mensagem de Erro */}
              {errorMessage && (
                <span className="error-msg">
                  <i className="fas fa-exclamation-circle"></i> {errorMessage}
                </span>
              )}
            </div>
          );
        })}

      </div>

      <button type="submit" className="submit-btn" disabled={isLoading}>
        {isLoading ? (
          <span><i className="fas fa-spinner fa-spin"></i> Processando...</span>
        ) : (
          submitText
        )}
      </button>
      
      {children}
    </form>
  );
};