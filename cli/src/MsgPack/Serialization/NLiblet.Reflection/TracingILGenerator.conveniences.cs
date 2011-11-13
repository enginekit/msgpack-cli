﻿#region -- License Terms --
//
// NLiblet
//
// Copyright (C) 2011 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;

namespace NLiblet.Reflection
{
	partial class TracingILGenerator
	{
		/// <summary>
		///		Emit 'call' or 'callvirt' appropriately.
		/// </summary>
		/// <param name="target"><see cref="MethodInfo"/> to be called.</param>
		public void EmitAnyCall( MethodInfo target )
		{
			Contract.Requires( target != null );

			if ( target.IsStatic )
			{
				this.EmitCall( target );
			}
			else
			{
				this.EmitCallvirt( target );
			}
		}

		/// <summary>
		///		Emit property getter invocation.
		///		Pre condition is there is target instance on the top of evaluation stack when <paramref name="property"/> is instance property.
		///		Post condition are that target instance will be removed from the stack for instance property, and property value will be placed on there.
		/// </summary>
		/// <param name="property"><see cref="PropertyInfo"/> for target property.</param>
		public void EmitGetProperty( PropertyInfo property )
		{
			Contract.Requires( property != null );
			Contract.Requires( property.CanRead );

			var getter = property.GetGetMethod( true );
			if ( getter.IsStatic )
			{
				this.EmitCall( getter );
			}
			else
			{
				this.EmitCallvirt( getter );
			}
		}

		private static readonly PropertyInfo _cultureInfo_CurrentCulture = typeof( CultureInfo ).GetProperty( "CurrentCulture" );
		private static readonly PropertyInfo _cultureInfo_InvariantCulture = typeof( CultureInfo ).GetProperty( "InvariantCulture" );

		/// <summary>
		///		Emit <see cref="CultureInfo.CurrentCulture"/> invocation.
		/// </summary>
		public void EmitCurrentCulture()
		{
			this.EmitGetProperty( _cultureInfo_CurrentCulture );
		}

		/// <summary>
		///		Emit <see cref="CultureInfo.InvariantCulture"/> invocation.
		/// </summary>
		public void EmitInvariantCulture()
		{
			this.EmitGetProperty( _cultureInfo_InvariantCulture );
		}

		private static readonly MethodInfo _Object_GetType = typeof( object ).GetMethod( "GetType" );

		/// <summary>
		///		Emit <see cref="Object.GetType"/> invocation.
		///		Pre condition is that target instance is placed on the top of evaluation stack.
		///		Post condition is that target instance will be replaced with <see cref="Type"/> of it.
		/// </summary>
		public void EmitGetType()
		{
			this.EmitCall( _Object_GetType );
		}

		/// <summary>
		///		Emit <see cref="String.Format(IFormatProvider,String,Object[])"/> invocation with <see cref="CultureInfo.InvariantCulture"/>.
		/// </summary>
		/// <param name="temporaryLocalArrayIndex">
		///		Index of temporary local variable index to store param array for <see cref="String.Format(IFormatProvider,String,Object[])"/>.
		///		Note that the type of local variable must be Object[].
		///	</param>
		///	<param name="formatLiteral">Forat string literal.</param>
		/// <param name="argumentLoadingEmitters">
		///		List of delegates to emittion of loading formatting parameter loading instruction. 
		///		Index of this array corresponds to index of formatting parameter.
		///		1st argument is this instance.
		///		Post condition is that exactly one storing element will be added on the top of evaluation stack.
		/// </param>
		public void EmitStringFormat( int temporaryLocalArrayIndex, string formatLiteral, params Action<TracingILGenerator>[] argumentLoadingEmitters )
		{
			Contract.Requires( formatLiteral != null );
			Contract.Requires( 0 < formatLiteral.Length );
			Contract.Requires( argumentLoadingEmitters != null );
			Contract.Requires( Contract.ForAll( argumentLoadingEmitters, item => item != null ) );

			this.EmitCurrentCulture();
			this.EmitLdstr( formatLiteral );
			this.EmitStringFormatArgumentAndCall( temporaryLocalArrayIndex, argumentLoadingEmitters );
		}

		/// <summary>
		///		Emit <see cref="String.Format(IFormatProvider,String,Object[])"/> invocation with <see cref="CultureInfo.CurrentCulture"/>.
		/// </summary>
		/// <param name="temporaryLocalArrayIndex">
		///		Index of temporary local variable index to store param array for <see cref="String.Format(IFormatProvider,String,Object[])"/>.
		///		Note that the type of local variable must be Object[].
		///	</param>
		/// <param name="resource">
		///		Type of resource accessor.
		/// </param>
		/// <param name="resourceKey">
		///		Key of rethis. Note that this method assumes that key equals to accessor property name.
		/// </param>
		/// <param name="argumentLoadingEmitters">
		///		List of delegates to emittion of loading formatting parameter loading instruction. 
		///		Index of this array corresponds to index of formatting parameter.
		///		1st argument is this instance.
		///		Post condition is that exactly one storing element will be added on the top of evaluation stack.
		/// </param>
		public void EmitStringFormat( int temporaryLocalArrayIndex, Type resource, string resourceKey, params Action<TracingILGenerator>[] argumentLoadingEmitters )
		{
			Contract.Requires( resource != null );
			Contract.Requires( resourceKey != null );
			Contract.Requires( !String.IsNullOrWhiteSpace( resourceKey ) );
			Contract.Requires( argumentLoadingEmitters != null );
			Contract.Requires( Contract.ForAll( argumentLoadingEmitters, item => item != null ) );

			this.EmitCurrentCulture();
			this.EmitGetProperty( resource.GetProperty( resourceKey, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static ) );
			this.EmitStringFormatArgumentAndCall( temporaryLocalArrayIndex, argumentLoadingEmitters );
		}

		/// <summary>
		///		Emit <see cref="String.Format(IFormatProvider,String,Object[])"/> invocation with <see cref="CultureInfo.InvariantCulture"/>.
		/// </summary>
		/// <param name="temporaryLocalArrayIndex">
		///		Index of temporary local variable index to store param array for <see cref="String.Format(IFormatProvider,String,Object[])"/>.
		///		Note that the type of local variable must be Object[].
		///	</param>
		///	<param name="formatLiteral">Forat string literal.</param>
		/// <param name="argumentLoadingEmitters">
		///		List of delegates to emittion of loading formatting parameter loading instruction. 
		///		Index of this array corresponds to index of formatting parameter.
		///		1st argument is this instance.
		///		Post condition is that exactly one storing element will be added on the top of evaluation stack.
		/// </param>
		public void EmitStringFormatInvariant( int temporaryLocalArrayIndex, string formatLiteral, params Action<TracingILGenerator>[] argumentLoadingEmitters )
		{
			Contract.Requires( formatLiteral != null );
			Contract.Requires( 0 < formatLiteral.Length );
			Contract.Requires( argumentLoadingEmitters != null );
			Contract.Requires( Contract.ForAll( argumentLoadingEmitters, item => item != null ) );

			this.EmitInvariantCulture();
			this.EmitLdstr( formatLiteral );
			this.EmitStringFormatArgumentAndCall( temporaryLocalArrayIndex, argumentLoadingEmitters );
		}

		/// <summary>
		///		Emit <see cref="String.Format(IFormatProvider,String,Object[])"/> invocation with <see cref="CultureInfo.InvariantCulture"/>.
		/// </summary>
		/// <param name="temporaryLocalArrayIndex">
		///		Index of temporary local variable index to store param array for <see cref="String.Format(IFormatProvider,String,Object[])"/>.
		///		Note that the type of local variable must be Object[].
		///	</param>
		/// <param name="resource">
		///		Type of resource accessor.
		/// </param>
		/// <param name="resourceKey">
		///		Key of rethis. Note that this method assumes that key equals to accessor property name.
		/// </param>
		/// <param name="argumentLoadingEmitters">
		///		List of delegates to emittion of loading formatting parameter loading instruction. 
		///		Index of this array corresponds to index of formatting parameter.
		///		1st argument is this instance.
		///		Post condition is that exactly one storing element will be added on the top of evaluation stack.
		/// </param>
		public void EmitStringFormatInvariant( int temporaryLocalArrayIndex, Type resource, string resourceKey, params Action<TracingILGenerator>[] argumentLoadingEmitters )
		{
			Contract.Requires( resource != null );
			Contract.Requires( resourceKey != null );
			Contract.Requires( !String.IsNullOrWhiteSpace( resourceKey ) );
			Contract.Requires( argumentLoadingEmitters != null );
			Contract.Requires( Contract.ForAll( argumentLoadingEmitters, item => item != null ) );

			this.EmitInvariantCulture();
			this.EmitGetProperty( resource.GetProperty( resourceKey, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static ) );
			this.EmitStringFormatArgumentAndCall( temporaryLocalArrayIndex, argumentLoadingEmitters );
		}

		private static readonly MethodInfo _string_Format = typeof( String ).GetMethod( "Format", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof( IFormatProvider ), typeof( string ), typeof( object[] ) }, null );

		private void EmitStringFormatArgumentAndCall( int temporaryLocalArrayIndex, Action<TracingILGenerator>[] argumentEmitters )
		{
			this.EmitNewarr( temporaryLocalArrayIndex, typeof( object ), argumentEmitters );
			this.EmitAnyLdloc( temporaryLocalArrayIndex );
			this.EmitCall( _string_Format );
		}

		/// <summary>
		///		Emit load 'this' pointer instruction (namely 'ldarg.0').
		///		Post condition is that the loaded value will be added on the evaluation stack.
		/// </summary>
		public void EmitLdargThis()
		{
			this.EmitLdarg_0();
		}

		/// <summary>
		///		Emit apprpriate 'ldarg.*' instruction.
		///		Post condition is that the loaded value will be added on the evaluation stack.
		/// </summary>
		/// <param name="argumentIndex">
		///		Index of argument to be fetched.
		///	</param>
		public void EmitAnyLdarg( int argumentIndex )
		{
			Contract.Requires( 0 <= argumentIndex && argumentIndex <= UInt16.MaxValue );

			switch ( argumentIndex )
			{
				case 0:
				{
					this.EmitLdarg_0();
					break;
				}
				case 1:
				{
					this.EmitLdarg_1();
					break;
				}
				case 2:
				{
					this.EmitLdarg_2();
					break;
				}
				case 3:
				{
					this.EmitLdarg_3();
					break;
				}
				default:
				{
					if ( argumentIndex <= Byte.MaxValue )
					{
						this.EmitLdarg_S( unchecked( ( byte )argumentIndex ) );
					}
					else
					{
						this.EmitLdarg( argumentIndex );
					}
					break;
				}
			}
		}

		/// <summary>
		///		Emit apprpriate 'ldloc.*' instruction.
		///		Post condition is that the loaded value will be added on the evaluation stack.
		/// </summary>
		/// <param name="localIndex">
		///		Index of local variable to be fetched.
		///	</param>
		public void EmitAnyLdloc( int localIndex )
		{
			Contract.Requires( 0 <= localIndex && localIndex <= UInt16.MaxValue );

			switch ( localIndex )
			{
				case 0:
				{
					this.EmitLdloc_0();
					break;
				}
				case 1:
				{
					this.EmitLdloc_1();
					break;
				}
				case 2:
				{
					this.EmitLdloc_2();
					break;
				}
				case 3:
				{
					this.EmitLdloc_3();
					break;
				}
				default:
				{
					if ( localIndex <= Byte.MaxValue )
					{
						this.EmitLdloc_S( unchecked( ( byte )localIndex ) );
					}
					else
					{
						this.EmitLdloc( localIndex );
					}
					break;
				}
			}
		}

		/// <summary>
		///		Emit array initialization code with initializer.
		///		Pre condition is that the storing value is placed on the top of evaluation stack and its type is valid.
		///		Post condition is that the stored value will be removed from the evaluation stack.
		/// </summary>
		/// <param name="localIndex">
		///		Index of local variable which stores the array.
		///	</param>
		public void EmitAnyStloc( int localIndex )
		{
			Contract.Requires( 0 <= localIndex && localIndex <= UInt16.MaxValue );

			switch ( localIndex )
			{
				case 0:
				{
					this.EmitStloc_0();
					break;
				}
				case 1:
				{
					this.EmitStloc_1();
					break;
				}
				case 2:
				{
					this.EmitStloc_2();
					break;
				}
				case 3:
				{
					this.EmitStloc_3();
					break;
				}
				default:
				{
					if ( localIndex <= Byte.MaxValue )
					{
						this.EmitStloc_S( unchecked( ( byte )localIndex ) );
					}
					else
					{
						this.EmitStloc( localIndex );
					}
					break;
				}
			}
		}

		/// <summary>
		///		Emit array initialization code without initializer.
		///		Post condition is evaluation stack will no be modified as previous state. 
		///		Note that initialized array is not placed on the top of evaluation stack.
		/// </summary>
		/// <param name="elementType"><see cref="Type"/> of array element. This can be generaic parameter.</param>
		/// <param name="length">Size of array.</param>
		public void EmitNewarr( Type elementType, long length )
		{
			Contract.Requires( elementType != null );
			Contract.Requires( 0 <= length );

			this.EmitNewarrCore( elementType, length );
		}

		/// <summary>
		///		Emit array initialization code with initializer.
		///		Post condition is evaluation stack will no be modified as previous state. 
		///		Note that initialized array is not placed on the top of evaluation stack.
		/// </summary>
		/// <param name="arrayLocalIndex">
		///		Index of local variable which stores the array.
		///	</param>
		/// <param name="elementType"><see cref="Type"/> of array element. This can be generaic parameter.</param>
		/// <param name="elementLoadingEmitters">
		///		List of delegates to emittion of storing element loading instruction. 
		///		Index of this array corresponds to index of initializing array.
		///		1st argument is this instance.
		///		Post condition is that exactly one storing element will be added on the top of stack and its type is <paramref name="elementType"/> compatible.
		/// </param>
		public void EmitNewarr( int arrayLocalIndex, Type elementType, params Action<TracingILGenerator>[] elementLoadingEmitters )
		{
			Contract.Requires( 0 <= arrayLocalIndex );
			Contract.Requires( elementType != null );
			Contract.Requires( elementLoadingEmitters != null );
			Contract.Requires( Contract.ForAll( elementLoadingEmitters, item => item != null ) );

			this.EmitNewarrCore( elementType, elementLoadingEmitters.LongLength );
			this.EmitAnyStloc( arrayLocalIndex );

			for ( long i = 0; i < elementLoadingEmitters.LongLength; i++ )
			{
				this.EmitAnyStelem( elementType, il => il.EmitAnyLdloc( arrayLocalIndex ), i, elementLoadingEmitters[ i ] );
			}
		}

		/// <summary>
		///		Emit array initialization code with initializer.
		///		Post condition is evaluation stack will no be modified as previous state. 
		///		Note that initialized array is not placed on the top of evaluation stack.
		/// </summary>
		/// <param name="arrayLoadingEmitter">
		///		Delegate to emittion of array loading instruction. 
		///		1st argument is this instance.
		///		Post condition is that exactly one target array will be added on the top of stack and element type is <paramref name="elementType"/>.
		///	</param>
		/// <param name="arrayStoringEmitter">
		///		Delegate to emittion of array storing instruction. 
		///		1st argument is this instance.
		///		Pre condition is that the top of evaluation stack is array type and its element type is <paramref name="elementType"/>.
		///		Post condition is that exactly one target array will be removed from the top of stack.
		/// </param>
		/// <param name="elementType"><see cref="Type"/> of array element. This can be generaic parameter.</param>
		/// <param name="elementLoadingEmitters">
		///		List of delegates to emittion of storing element loading instruction. 
		///		Index of this array corresponds to index of initializing array.
		///		1st argument is this instance.
		///		Post condition is that exactly one storing element will be added on the top of stack and its type is <paramref name="elementType"/> compatible.
		/// </param>
		public void EmitNewarr( Action<TracingILGenerator> arrayLoadingEmitter, Action<TracingILGenerator> arrayStoringEmitter, Type elementType, params Action<TracingILGenerator>[] elementLoadingEmitters )
		{
			Contract.Requires( arrayLoadingEmitter != null );
			Contract.Requires( arrayStoringEmitter != null );
			Contract.Requires( elementType != null );
			Contract.Requires( elementLoadingEmitters != null );
			Contract.Requires( Contract.ForAll( elementLoadingEmitters, item => item != null ) );

			this.EmitNewarrCore( elementType, elementLoadingEmitters.LongLength );
			arrayStoringEmitter( this );

			for ( long i = 0; i < elementLoadingEmitters.LongLength; i++ )
			{
				arrayLoadingEmitter( this );
				this.EmitAnyStelem( elementType, null, i, elementLoadingEmitters[ i ] );
			}
		}

		private void EmitNewarrCore( Type elementType, long length )
		{
			this.EmitLiteralInteger( length );
			this.EmitNewarr( elementType );
		}

		/// <summary>
		///		Emit array element loading instructions. 
		///		Post condition is that exactly one loaded element will be placed on the top of stack and its element type is <paramref name="elementType"/>.
		/// </summary>
		/// <param name="elementType"><see cref="Type"/> of array element. This can be generaic parameter.</param>
		/// <param name="arrayLoadingEmitter">
		///		Delegate to emittion of array loading instruction. 
		///		1st argument is this instance.
		///		Post condition is that exactly one target array will be added on the top of stack and its element type is <paramref name="elementType"/>.
		///	</param>
		/// <param name="index">Index of array element.</param>
		public void EmitAnyLdelem( Type elementType, Action<TracingILGenerator> arrayLoadingEmitter, long index )
		{
			Contract.Requires( elementType != null );
			Contract.Requires( 0 <= index );
			Contract.Requires( arrayLoadingEmitter != null );

			arrayLoadingEmitter( this );
			this.EmitLiteralInteger( index );

			if ( elementType.IsGenericParameter )
			{
				// T
				this.EmitLdelem( elementType );
				return;
			}

			if ( !elementType.IsValueType )
			{
				// ref
				this.EmitLdelem_Ref();
				return;
			}

			switch ( Type.GetTypeCode( elementType ) )
			{
				case TypeCode.Boolean:
				case TypeCode.SByte:
				{
					this.EmitLdelem_I1();
					break;
				}
				case TypeCode.Int16:
				{
					this.EmitLdelem_I2();
					break;
				}
				case TypeCode.Int32:
				{
					this.EmitLdelem_I4();
					break;
				}
				case TypeCode.Int64:
				case TypeCode.UInt64:
				{
					this.EmitLdelem_I8();
					break;
				}
				case TypeCode.Byte:
				{
					this.EmitLdelem_U1();
					break;
				}
				case TypeCode.UInt16:
				case TypeCode.Char:
				{
					this.EmitLdelem_U2();
					break;
				}
				case TypeCode.UInt32:
				{
					this.EmitLdelem_U4();
					break;
				}
				case TypeCode.Single:
				{
					this.EmitLdelem_R4();
					break;
				}
				case TypeCode.Double:
				{
					this.EmitLdelem_R8();
					break;
				}
				default:
				{
					// Other value type	
					this.EmitLdelema( elementType );

					this.EmitLdobj( elementType );
					break;
				}
			}
		}

		/// <summary>
		///		Emit array element storing instructions.
		///		Post condition is evaluation stack will no be modified as previous state.
		/// </summary>
		/// <param name="elementType"><see cref="Type"/> of array element. This can be generaic parameter.</param>
		/// <param name="arrayLoadingEmitter">
		///		Delegate to emittion of array loading instruction. 
		///		1st argument is this instance.
		///		Post condition is that exactly one target array will be added on the top of stack and its element type is <paramref name="elementType"/>.
		///	</param>
		/// <param name="index">Index of array element.</param>
		/// <param name="elementLoadingEmitter">
		///		Delegate to emittion of storing element loading instruction. 
		///		1st argument is this instance.
		///		Post condition is that exactly one storing element will be added on the top of stack and its type is <paramref name="elementType"/> compatible.
		/// </param>
		public void EmitAnyStelem( Type elementType, Action<TracingILGenerator> arrayLoadingEmitter, long index, Action<TracingILGenerator> elementLoadingEmitter )
		{
			Contract.Requires( elementType != null );
			Contract.Requires( 0 <= index );
			Contract.Requires( arrayLoadingEmitter != null );
			Contract.Requires( elementLoadingEmitter != null );

			arrayLoadingEmitter( this );
			this.EmitLiteralInteger( index );
			elementLoadingEmitter( this );

			if ( elementType.IsGenericParameter )
			{
				this.EmitStelem( elementType );
				return;
			}

			if ( !elementType.IsValueType )
			{
				// ref
				this.EmitStelem_Ref();
				return;
			}

			switch ( Type.GetTypeCode( elementType ) )
			{
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
				{
					this.EmitStelem_I1();
					break;
				}
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Char:
				{
					this.EmitStelem_I2();
					break;
				}
				case TypeCode.Int32:
				case TypeCode.UInt32:
				{
					this.EmitStelem_I4();
					break;
				}
				case TypeCode.Int64:
				case TypeCode.UInt64:
				{
					this.EmitStelem_I8();
					break;
				}
				case TypeCode.Single:
				{
					this.EmitStelem_R4();
					break;
				}
				case TypeCode.Double:
				{
					this.EmitStelem_R8();
					break;
				}
				default:
				{
					// Other value type	
					this.EmitLdelema( elementType );

					elementLoadingEmitter( this );

					this.EmitStobj( elementType );
					break;
				}
			}
		}

		/// <summary>
		///		Emit efficient integer constant loading.
		///		Post condition is that exactly one integer will be added on the top of stack.
		/// </summary>
		/// <param name="value">Integer value.</param>
		public void EmitLiteralInteger( long value )
		{
			switch ( value )
			{
				case -1:
				{
					this.EmitLdc_I4_M1();
					break;
				}
				case 0:
				{
					this.EmitLdc_I4_0();
					break;
				}
				case 1:
				{
					this.EmitLdc_I4_1();
					break;
				}
				case 2:
				{
					this.EmitLdc_I4_2();
					break;
				}
				case 3:
				{
					this.EmitLdc_I4_3();
					break;
				}
				case 4:
				{
					this.EmitLdc_I4_4();
					break;
				}
				case 5:
				{
					this.EmitLdc_I4_5();
					break;
				}
				case 6:
				{
					this.EmitLdc_I4_6();
					break;
				}
				case 7:
				{
					this.EmitLdc_I4_7();
					break;
				}
				case 8:
				{
					this.EmitLdc_I4_8();
					break;
				}
				default:
				{
					if ( SByte.MinValue <= value && value <= SByte.MaxValue )
					{
						this.EmitLdc_I4_S( unchecked( ( byte )value ) );
					}
					else if ( Int32.MinValue <= value && value <= Int32.MaxValue )
					{
						this.EmitLdc_I4( unchecked( ( int )value ) );
					}
					else
					{
						this.EmitLdc_I8( value );
					}

					break;
				}
			}
		}

		// TODO:Literal R4/R8
		// TODO:Literal Decimal
		// TODO:default(T)

		private static readonly MethodInfo _type_GetTypeFromHandle = typeof( Type ).GetMethod( "GetTypeFromHandle", new Type[] { typeof( RuntimeTypeHandle ) } );

		/// <summary>
		///		Emit 'typeof' expression.
		///		Post condition is <see cref="Type"/> instance for <paramref name="type"/> will be placed on the top of evaluation stack.
		/// </summary>
		/// <param name="type">Target <see cref="Type"/>.</param>
		public void EmitTypeOf( Type type )
		{
			Contract.Requires( type != null );

			this.EmitLdtoken( type );
			this.EmitCall( _type_GetTypeFromHandle );
		}

		private static readonly ConstructorInfo _ArgumentException_ctor_String_String_Exception =
			typeof( ArgumentException ).GetConstructor( new[] { typeof( String ), typeof( String ), typeof( Exception ) } );

		/// <summary>
		///		Emit 'throw new ArgumentException(String,String,Exception)' statement.
		///		Pre condition is that there are exactly three entries in the evaluation stack,
		///		which are string, string, and Exception instance.
		///		Post condition is that the evaluation statck will be empty.
		/// </summary>
		public void EmitThrowNewArgumentExceptionWithInnerException()
		{
			this.EmitNewobj( _ArgumentException_ctor_String_String_Exception );
			this.EmitThrow();
		}

		//  TODO:ArgumentNullException
		//  TODO:ArgumentOutOfRangeException

		private static readonly Type[] _standardExceptionConstructorParamterTypesWithInnerException = new[] { typeof( string ), typeof( Exception ) };

		/// <summary>
		///		Emit 'throw new TException(String,String,Exception)' statement.
		///		Pre condition is that there are exactly two entries in the evaluation stack,
		///		which are string and Exception instance.
		///		Post condition is that the evaluation statck will be empty.
		/// </summary>
		/// <param name="exceptionType"><see cref="Type"/> of initializing and throwing <see cref="Exception"/>.</param>
		public void EmitThrowNewExceptionWithInnerException( Type exceptionType )
		{
			Contract.Requires( exceptionType != null );

			var ctor = exceptionType.GetConstructor( _standardExceptionConstructorParamterTypesWithInnerException );
			if ( ctor == null )
			{
				throw new NotSupportedException( String.Format( CultureInfo.CurrentCulture, "Exception type '{0}' does not have standard constructor '.ctor(String, Exception)'.", exceptionType ) );
			}

			this.EmitNewobj( ctor );
			this.EmitThrow();
		}
	}
}
